using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using MonteOracionApp.Data;
using MonteOracionApp.Modules.Congregants;
using MonteOracionApp.Modules.Donations;
using MonteOracionApp.Modules.Inventory;
using MonteOracionApp.Modules.Reports;
using MonteOracionApp.Modules.Staff;
using MonteOracionApp.Modules.Treasury;
using MonteOracionApp.Shared;

string FindProject(){var dir=new DirectoryInfo(AppContext.BaseDirectory);while(dir!=null){if(File.Exists(Path.Combine(dir.FullName,"MonteOracionApp.csproj")))return dir.FullName;dir=dir.Parent;}throw new Exception("Carpeta de proyecto no encontrada.");}
var project=FindProject();
var output=Path.Combine(project,"artifacts","checks");Directory.CreateDirectory(output);
var database="MonteOracionApp_Verification_"+Guid.NewGuid().ToString("N");
var connection=$"Server={Environment.GetEnvironmentVariable("MONTE_TEST_SERVER")??"DESKTOP-PP9ER99"};Database={database};Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=10";
var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
ApplicationDbContext Db()=>new(options);
var storage=new FileStorage(new TestEnvironment(),new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Storage:Path",Path.Combine(output,database)}}).Build());
InventoryService Inv(ApplicationDbContext db)=>new(db,storage);
TreasuryService Treasury(ApplicationDbContext db)=>new(db,storage);
DonationService Donations(ApplicationDbContext db)=>new(db,storage,Inv(db),Treasury(db));
IFormFile Photo()=>new FormFile(new MemoryStream(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jfXkAAAAASUVORK5CYII=")),0,68,"file","foto.png");
var passed=0;var finished=false;
async Task Check(string name,Func<Task> check){await check();passed++;Console.WriteLine("OK "+name);}
void Assert(bool condition){if(!condition)throw new Exception("Comprobación fallida.");}
async Task Failure(Func<Task> action){try{await action();}catch(InvalidOperationException){return;}throw new Exception("La operación debía rechazarse.");}
async Task Use(Func<ApplicationDbContext,Task> action){await using var db=Db();await action(db);}
var password="Prueba-"+Guid.NewGuid().ToString("N");
var admin="legacy-admin";var treasury="test-treasury";var warehouse="test-warehouse";
int productId=0,beneficiaryId=0,lotId=0,requestId=0,lineId=0,loanId=0,loanLine=0,moneyId=0,advanceId=0;
try
{
    await Check("Migración conserva datos anteriores y traslada donaciones aprobadas al libro",async()=>{
        await using var db=Db();
        await db.GetService<IMigrator>().MigrateAsync("20261012013447_RenombrarTablasEspanol");
        await db.Database.ExecuteSqlRawAsync("""
INSERT INTO tb_usuario (Id,UserName,NormalizedUserName,Email,NormalizedEmail,EmailConfirmed,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount)
VALUES ('legacy-admin','admin@test.local','ADMIN@TEST.LOCAL','admin@test.local','ADMIN@TEST.LOCAL',1,0,0,1,0);
INSERT INTO tb_rol(Id,Name,NormalizedName) VALUES ('legacy-admin-role',N'Administrador',N'ADMINISTRADOR');
INSERT INTO tb_usuario_rol(UserId,RoleId) VALUES ('legacy-admin','legacy-admin-role');
INSERT INTO tb_congregante(FirstName,LastName,IsActive) VALUES(N'Persona',N'Anterior',1);
INSERT INTO tb_donacion(TrackingCode,Kind,Status,DonorKind,Amount,ReceivedOrPaidOn,CreatedAt,RecordedByUserId)
VALUES(NEWID(),0,1,2,100,CAST(GETDATE() AS date),SYSUTCDATETIME(),'legacy-admin');
""");
        await db.Database.MigrateAsync();
        Assert(await db.StaffProfiles.AnyAsync(x=>x.UserId==admin));
        Assert(await db.Donations.CountAsync()==1&&await db.MoneyMovements.SumAsync(x=>x.Amount)==100);
        Assert((await db.Congregants.SingleAsync()).Dni==null);
        Assert(!db.Database.HasPendingModelChanges());
    });
    await Use(async db=>{
        var hasher=new PasswordHasher<IdentityUser>();
        var a=await db.Users.SingleAsync(x=>x.Id==admin);a.PasswordHash=hasher.HashPassword(a,password);a.SecurityStamp=Guid.NewGuid().ToString();
        foreach(var tuple in new[]{(treasury,"tesorero@test.local",Roles.Treasurer),(warehouse,"almacen@test.local",Roles.Warehouse)})
        {
            var user=new IdentityUser{Id=tuple.Item1,UserName=tuple.Item2,NormalizedUserName=tuple.Item2.ToUpperInvariant(),Email=tuple.Item2,NormalizedEmail=tuple.Item2.ToUpperInvariant(),EmailConfirmed=true,SecurityStamp=Guid.NewGuid().ToString(),LockoutEnabled=true};
            user.PasswordHash=hasher.HashPassword(user,password);db.Users.Add(user);
            var role=new IdentityRole(tuple.Item3){Id=Guid.NewGuid().ToString(),NormalizedName=tuple.Item3.ToUpperInvariant()};db.Roles.Add(role);db.UserRoles.Add(new IdentityUserRole<string>{UserId=user.Id,RoleId=role.Id});
            db.StaffProfiles.Add(new StaffProfile{UserId=user.Id,FullName=tuple.Item3+" de prueba"});
        }
        var c=await db.Congregants.SingleAsync();c.Dni="87654321";
        db.StaffProfiles.Single(x=>x.UserId==admin).FullName="Administrador de prueba";
        var product=new Product{Name="Arroz de prueba",Unit="kg",Category="Alimentos",RequiresExpiry=true};db.Products.Add(product);
        var beneficiary=new Beneficiary{Name="Albergue de prueba",Address="San Juan",Contact="Responsable de prueba"};db.Beneficiaries.Add(beneficiary);
        db.PaymentMethods.Add(new MonteOracionApp.Modules.Settings.PaymentMethod{Name="Yape de prueba",Instructions="Datos de prueba",IsActive=true});
        await db.SaveChangesAsync();productId=product.Id;beneficiaryId=beneficiary.Id;
    });
    await Check("Recepción de bienes crea lote, entrada y trazabilidad atómicamente",async()=>{
        await Use(async db=>{
            var d=await Donations(db).RegisterReceivedAsync(new ReceivedDonationForm{Kind=DonationKind.Bienes,DonorKind=DonorKind.Anonimo,ConfirmReceived=true,ReceivedOn=ChurchTime.Today,ItemPhoto=Photo(),Items=[new ReceivedItemForm{ProductId=productId,Quantity=10,ExpiresOn=ChurchTime.Today.AddMonths(2),StorageLocation="Estante A"}]},warehouse);
            var lot=await db.StockLots.SingleAsync(x=>x.DonationItem!.DonationId==d.Id);lotId=lot.Id;
            Assert(lot.OnHand==10&&lot.Reserved==0&&await db.StockMovements.CountAsync(x=>x.StockLotId==lotId)==1);
        });
    });
    await Check("Alimentos sin vencimiento no entran al inventario",()=>Use(async db=>{
        await Failure(()=>Donations(db).RegisterReceivedAsync(new ReceivedDonationForm{Kind=DonationKind.Bienes,DonorKind=DonorKind.Anonimo,ConfirmReceived=true,ReceivedOn=ChurchTime.Today,ItemPhoto=Photo(),Items=[new ReceivedItemForm{ProductId=productId,Quantity=1,StorageLocation="A"}]},warehouse));
    }));
    StockRequestForm Request(decimal quantity,StockRequestKind kind=StockRequestKind.Entrega)=>new(){BeneficiaryId=beneficiaryId,Purpose="Necesidad sustentada de prueba",NeededOn=ChurchTime.Today,Kind=kind,ReturnDueOn=kind==StockRequestKind.Prestamo?ChurchTime.Today.AddDays(7):null,Lines=[new StockLineForm{StockLotId=lotId,Quantity=quantity}]};
    await Use(async db=>requestId=await Inv(db).RequestAsync(Request(6),treasury));
    await Check("Una persona no aprueba su propia solicitud de bienes",()=>Use(db=>Failure(()=>Inv(db).DecideAsync(requestId,true,"",treasury))));
    await Check("Aprobar reserva stock y bloquea una segunda promesa del mismo bien",async()=>{
        int other=0;await Use(async db=>other=await Inv(db).RequestAsync(Request(6),warehouse));
        await Use(db=>Inv(db).DecideAsync(requestId,true,"Prioridad verificada",admin));
        await Use(db=>Failure(()=>Inv(db).DecideAsync(other,true,"",admin)));
        await Use(async db=>{var lot=await db.StockLots.SingleAsync(x=>x.Id==lotId);Assert(lot.OnHand==10&&lot.Reserved==6);lineId=await db.StockRequestLines.Where(x=>x.StockRequestId==requestId).Select(x=>x.Id).SingleAsync();});
    });
    await Check("Entrega parcial reduce físico y reserva; repetir formulario no duplica salida",async()=>{
        var command=Guid.NewGuid();await Use(db=>Inv(db).DeliverAsync(lineId,2,"Receptor de prueba",Photo(),warehouse,command));
        await Use(db=>Failure(()=>Inv(db).DeliverAsync(lineId,2,"Receptor de prueba",Photo(),warehouse,command)));
        await Use(async db=>{var lot=await db.StockLots.SingleAsync(x=>x.Id==lotId);Assert(lot.OnHand==8&&lot.Reserved==4);});
    });
    await Check("Cancelar libera solamente lo pendiente y conserva la entrega",async()=>{
        await Use(db=>Inv(db).CancelAsync(requestId,"Cambio de programación",admin,true));
        await Use(async db=>{var lot=await db.StockLots.SingleAsync(x=>x.Id==lotId);Assert(lot.OnHand==8&&lot.Reserved==0);});
    });
    await Check("Préstamo y reposición generan movimientos separados y saldo pendiente",async()=>{
        await Use(async db=>loanId=await Inv(db).RequestAsync(Request(4,StockRequestKind.Prestamo),treasury));
        await Use(db=>Inv(db).DecideAsync(loanId,true,"Préstamo desde stock libre",admin));
        await Use(async db=>loanLine=await db.StockRequestLines.Where(x=>x.StockRequestId==loanId).Select(x=>x.Id).SingleAsync());
        await Use(db=>Inv(db).DeliverAsync(loanLine,3,"Responsable préstamo",Photo(),warehouse,Guid.NewGuid()));
        await Use(db=>Inv(db).CancelAsync(loanId,"No se entregará la cuarta unidad",admin,true));
        var command=Guid.NewGuid();
        await Use(db=>Inv(db).ReturnAsync(loanLine,1,"Reposición en buen estado","Estante B",ChurchTime.Today.AddMonths(3),warehouse,command));
        await Use(db=>Failure(()=>Inv(db).ReturnAsync(loanLine,1,"Duplicado","B",ChurchTime.Today.AddMonths(3),warehouse,command)));
        await Use(async db=>{var line=await db.StockRequestLines.SingleAsync(x=>x.Id==loanLine);Assert(line.Delivered-line.Returned-line.WrittenOff==2);Assert(await db.StockLots.SumAsync(x=>x.OnHand)==6);});
    });
    await Check("Condonar deja historial sin aumentar existencias",async()=>{
        await Use(db=>Inv(db).WriteOffAsync(loanLine,1,"Autorización excepcional sustentada",admin,Guid.NewGuid()));
        await Use(async db=>Assert(await db.StockLots.SumAsync(x=>x.OnHand)==6));
    });
    await Check("Un ajuste no puede consumir existencias reservadas",async()=>{
        int r=0;await Use(async db=>r=await Inv(db).RequestAsync(Request(4),warehouse));
        await Use(db=>Inv(db).DecideAsync(r,true,"",admin));
        await Use(db=>Failure(()=>Inv(db).AdjustAsync(lotId,-2,"Merma observada",admin,true,Guid.NewGuid())));
        await Use(db=>Inv(db).CancelAsync(r,"Finaliza comprobación",admin,true));
    });
    await Check("Autorización de dinero requiere otra persona y reserva sin pagar",async()=>{
        await Use(async db=>moneyId=await Treasury(db).RequestAsync(new FinanceForm{Amount=30,FundId=1,Kind=FinanceKind.Gasto,Channel=MoneyChannel.Efectivo,Purpose="Compra de prueba",Payee="Proveedor de prueba"},treasury,false));
        await Use(db=>Failure(()=>Treasury(db).DecideAsync(moneyId,true,"",treasury)));
        await Use(db=>Treasury(db).DecideAsync(moneyId,true,"Compra autorizada",admin));
        await Use(async db=>{Assert(await db.MoneyMovements.SumAsync(x=>x.Amount)==100);Assert(await Treasury(db).AvailableAsync(1,MoneyChannel.Efectivo)==70);});
    });
    await Check("Pago real baja dinero una sola vez",async()=>{
        await Use(db=>Treasury(db).PayAsync(moneyId,Photo(),treasury));
        await Use(db=>Failure(()=>Treasury(db).PayAsync(moneyId,Photo(),treasury)));
        await Use(async db=>Assert(await db.MoneyMovements.SumAsync(x=>x.Amount)==70));
    });
    await Check("Rendición del anticipo devuelve sobrante sin duplicar gasto",async()=>{
        await Use(async db=>advanceId=await Treasury(db).RequestAsync(new FinanceForm{Amount=20,FundId=1,Kind=FinanceKind.Anticipo,Channel=MoneyChannel.Efectivo,Purpose="Compra con rendición",Payee="Encargado",DueOn=ChurchTime.Today.AddDays(2)},warehouse,false));
        await Use(db=>Treasury(db).DecideAsync(advanceId,true,"",admin));await Use(db=>Treasury(db).PayAsync(advanceId,Photo(),treasury));
        await Use(db=>Treasury(db).SettleAsync(advanceId,5,Photo(),treasury));
        await Use(async db=>Assert(await db.MoneyMovements.SumAsync(x=>x.Amount)==65));
    });
    await Check("Dos aprobaciones simultáneas no reservan más stock del disponible",async()=>{
        int first=0,second=0;await Use(async db=>first=await Inv(db).RequestAsync(Request(4),treasury));await Use(async db=>second=await Inv(db).RequestAsync(Request(4),warehouse));
        async Task<bool> Race(int id){try{await Use(db=>Inv(db).DecideAsync(id,true,"Concurrencia",admin));return true;}catch(InvalidOperationException){return false;}catch(DbUpdateException){return false;}catch(Microsoft.Data.SqlClient.SqlException){return false;}}
        var result=await Task.WhenAll(Race(first),Race(second));Assert(result.Count(x=>x)==1);
        await Use(async db=>{var lot=await db.StockLots.SingleAsync(x=>x.Id==lotId);Assert(lot.Reserved==4&&lot.OnHand==5);});
    });
    await Check("DNI vincula donación digital; número de operación duplicado se rechaza",async()=>{
        await Use(async db=>{
            var payment=await db.PaymentMethods.SingleAsync();
            var form=new OnlineDonationForm{Dni="87654321",Amount=10,FundId=1,PaymentMethodId=payment.Id,OperationNumber="TEST-0001",PaidOn=ChurchTime.Today,Receipt=Photo()};
            var d=await Donations(db).RegisterOnlineAsync(form,null);Assert(d.Status==DonationStatus.Pendiente&&d.CongregantId!=null);
        });
        await Use(async db=>{
            var payment=await db.PaymentMethods.SingleAsync();
            await Failure(()=>Donations(db).RegisterOnlineAsync(new OnlineDonationForm{Dni="87654321",Amount=10,FundId=1,PaymentMethodId=payment.Id,OperationNumber="test-0001",PaidOn=ChurchTime.Today,Receipt=Photo()},null));
        });
    });
    await Check("Confirmar pago digital crea entrada; anular revierte con historial",async()=>{
        int id=0;await Use(async db=>{var d=await db.Donations.SingleAsync(x=>x.OperationNumber=="TEST-0001");id=d.Id;await Donations(db).DecideAsync(id,DonationStatus.Aprobada,null,treasury,d.Version);});
        await Use(async db=>{Assert(await Treasury(db).AvailableAsync(1,MoneyChannel.Digital)==10);var d=await db.Donations.SingleAsync(x=>x.Id==id);await Donations(db).DecideAsync(id,DonationStatus.Anulada,"Corrección de prueba",admin,d.Version);});
        await Use(async db=>Assert(await Treasury(db).AvailableAsync(1,MoneyChannel.Digital)==0));
    });
    await Check("Cierre exige explicación de diferencia y bloquea nuevos movimientos de esa fecha",async()=>{
        await Use(db=>Failure(()=>Treasury(db).CloseAsync(ChurchTime.Today,64,"",treasury,MoneyChannel.Efectivo,null)));
        await Use(db=>Treasury(db).CloseAsync(ChurchTime.Today,64,"Falta un sol; solicitar ajuste en fecha abierta",treasury,MoneyChannel.Efectivo,null));
        await Use(db=>Failure(()=>Treasury(db).DonationAsync(new Donation{Kind=DonationKind.Dinero,Amount=1,FundId=1},treasury)));
        await Use(async db=>{var c=await db.CashClosings.SingleAsync();Assert(c.Expected==65&&c.Counted==64);await Failure(()=>Treasury(db).ReviewClosingAsync(c.Id,treasury));});
        await Use(async db=>{var c=await db.CashClosings.SingleAsync();await Treasury(db).ReviewClosingAsync(c.Id,admin);});
    });
    await Check("Conciliación digital exige sustento y mantiene su cierre independiente",async()=>{
        await Use(db=>Failure(()=>Treasury(db).CloseAsync(ChurchTime.Today,0,"",treasury,MoneyChannel.Digital,null)));
        await Use(db=>Treasury(db).CloseAsync(ChurchTime.Today,0,"Estado de cuentas revisado",treasury,MoneyChannel.Digital,Photo()));
        await Use(async db=>Assert(await db.CashClosings.CountAsync()==2));
    });
    await Check("Cambiar datos del beneficiario conserva el destino original de solicitudes",async()=>{
        await Use(async db=>{var b=await db.Beneficiaries.SingleAsync();b.Address="Dirección nueva de prueba";await db.SaveChangesAsync();});
        await Use(async db=>Assert((await db.StockRequests.FirstAsync()).BeneficiaryAddress=="San Juan"));
    });
    await Check("PDF multipágina con acentos y texto largo",async()=>{
        var pdf=PdfReport.Create("Rendición de donaciones","Comprobación de presentación",Enumerable.Range(1,135).Select(i=>$"Registro {i} | arroz, leche, instrumentos | S/ 123.45 | responsable: José Núñez. "+new string('x',110)));
        Assert(System.Text.Encoding.ASCII.GetString(pdf,0,8)=="%PDF-1.4");
        await File.WriteAllBytesAsync(Path.Combine(output,"reporte-prueba.pdf"),pdf);
    });
    await Use(async db=>{
        var c=await db.Congregants.SingleAsync();
        var p=new MonteOracionApp.Modules.Publications.Publication{Kind=MonteOracionApp.Modules.Publications.PublicationKind.Evento,Title="Evento de prueba",Body="Descripción pública",Location="Comas",EventAt=ChurchTime.Today.AddDays(2).AddHours(9),EventEndsAt=ChurchTime.Today.AddDays(2).AddHours(12),IsPublished=true};
        db.Publications.Add(p);db.EventParticipants.Add(new(){Publication=p,CongregantId=c.Id,InternalNote="NOTA-PRIVADA-NO-PUBLICAR",RecordedBy=admin});
        await db.SaveChangesAsync();
    });
    await File.WriteAllTextAsync(Path.Combine(output,"fixture.json"),JsonSerializer.Serialize(new{database,connection,password,today=ChurchTime.Today.ToString("yyyy-MM-dd"),admin="admin@test.local",treasurer="tesorero@test.local",warehouse="almacen@test.local",dni="87654321"}));
    finished=true;Console.WriteLine($"{passed} comprobaciones integradas superadas. Base aislada: {database}");
}
finally
{
    if(!finished||Environment.GetEnvironmentVariable("MONTE_KEEP_TEST_DB")!="1")
    {
        if(!database.StartsWith("MonteOracionApp_Verification_")||database.Length!=61)throw new Exception("Nombre de base de prueba inválido para limpieza.");
        await using var cleanup=Db();await cleanup.Database.EnsureDeletedAsync();Console.WriteLine("Base aislada eliminada.");
    }
}
sealed class TestEnvironment:IWebHostEnvironment
{
    public string ApplicationName{get;set;}="Checks";
    public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();
    public string WebRootPath{get;set;}=AppContext.BaseDirectory;
    public string EnvironmentName{get;set;}="Development";
    public string ContentRootPath{get;set;}=AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
}
