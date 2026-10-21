document.querySelectorAll('aside a').forEach(a=>{if(a.href===location.href)a.setAttribute('aria-current','page');});
document.querySelectorAll('form').forEach(form=>form.addEventListener('submit',()=>{form.querySelectorAll('button[type=submit]').forEach(b=>{b.disabled=true;b.textContent='Procesando…';});}));
