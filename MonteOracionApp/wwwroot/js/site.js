document.querySelectorAll('form[data-confirm]').forEach(form => form.addEventListener('submit', event => {
    if (!window.confirm(form.dataset.confirm)) event.preventDefault();
}));
document.querySelectorAll('[data-repeat-add]').forEach(button => button.addEventListener('click', () => {
    const container = button.previousElementSibling;
    const rows = container.querySelectorAll('[data-repeat-row]');
    if (!rows.length || rows.length >= 50) return;
    const row = rows[0].cloneNode(true);
    row.querySelector('details')?.removeAttribute('open');
    row.querySelectorAll('input,select,textarea').forEach(field => {
        field.name = field.name.replace(/\[\d+\]/, `[${rows.length}]`);
        field.removeAttribute('id');
        field.disabled = false;
        if (field.tagName === 'SELECT') field.selectedIndex = 0;
        else if (field.type === 'checkbox' || field.type === 'radio') field.checked = false;
        else field.value = '';
    });
    row.querySelectorAll('[data-valmsg-for]').forEach(field => field.remove());
    container.append(row);
}));

/* Secciones visibles según radios (¿Qué se recibió? / ¿Quién dona?). */
function toggleRadioFields(name, attribute) {
    const checked = document.querySelector(`input[name="${name}"]:checked`);
    if (!checked) return;
    document.querySelectorAll(`[${attribute}]`).forEach(section => {
        const visible = section.getAttribute(attribute) === checked.value;
        section.hidden = !visible;
        section.querySelectorAll('input,select,textarea').forEach(field => field.disabled = !visible);
    });
}
['Kind', 'DonorKind'].forEach(name => {
    const radios = document.querySelectorAll(`input[name="${name}"]`);
    if (!radios.length) return;
    radios.forEach(radio => radio.addEventListener('change', () => toggleRadioFields(name, name === 'Kind' ? 'data-kind' : 'data-donor')));
    toggleRadioFields(name, name === 'Kind' ? 'data-kind' : 'data-donor');
});

/* ===== Recepción: buscador de miembro, filas de bienes y productos ===== */
const receive = document.querySelector('[data-receive]');
if (receive) {
    const catalog = JSON.parse(receive.querySelector('[data-receive-data]')?.textContent || '{}');
    const members = catalog.members || [];
    const products = catalog.products || [];
    const norm = text => (text || '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    const lev = (a, b) => {
        const rows = Array.from({ length: a.length + 1 }, (_, i) => [i, ...Array(b.length).fill(0)]);
        for (let j = 0; j <= b.length; j++) rows[0][j] = j;
        for (let i = 1; i <= a.length; i++) for (let j = 1; j <= b.length; j++)
            rows[i][j] = Math.min(rows[i - 1][j] + 1, rows[i][j - 1] + 1, rows[i - 1][j - 1] + (a[i - 1] === b[j - 1] ? 0 : 1));
        return rows[a.length][b.length];
    };
    const similar = (a, b) => {
        const x = norm(a), y = norm(b);
        if (!x || !y) return false;
        if (x === y) return true;
        return Math.max(x.length, y.length) >= 4 && lev(x, y) <= 2;
    };

    /* --- Miembro: DNI o nombre --- */
    const mSearch = receive.querySelector('[data-member-search]');
    const mList = receive.querySelector('[data-member-list]');
    const mId = receive.querySelector('[data-member-id]');
    const mChip = receive.querySelector('[data-member-chip]');
    const mName = receive.querySelector('[data-member-name]');
    const newMember = receive.querySelector('[data-new-member]');
    const memberInput = () => newMember?.querySelectorAll('input').forEach(field => field.value = '');
    const memberClear = () => {
        mId.value = '';
        mChip.hidden = true;
        mSearch.disabled = false;
        mSearch.focus();
    };
    const memberSelect = id => {
        const member = members.find(x => String(x.id) === String(id));
        if (!member) return;
        mId.value = member.id;
        mName.textContent = `${member.name}${member.dni ? ` · DNI ${member.dni}` : ''}`;
        mChip.hidden = false;
        mSearch.value = '';
        mSearch.disabled = true;
        mList.hidden = true;
        if (newMember) newMember.hidden = true;
        memberInput();
    };
    mSearch?.addEventListener('input', () => {
        const typed = mSearch.value.trim();
        if (mId.value) memberClear();
        if (!typed) { mList.hidden = true; return; }
        const digits = typed.replace(/\D/g, '');
        const q = norm(typed);
        const matches = members.filter(member => digits.length >= 2
            ? (member.dni || '').includes(digits)
            : norm(member.name).includes(q)).slice(0, 8);
        mList.innerHTML = matches.map(member =>
            `<button type="button" class="list-group-item list-group-item-action py-1" data-member-option="${member.id}">${member.name}${member.dni ? ` · DNI ${member.dni}` : ''}</button>`).join('')
            + '<button type="button" class="list-group-item list-group-item-action py-1 text-primary" data-member-new>➕ Nuevo miembro…</button>';
        mList.hidden = false;
    });
    mList?.addEventListener('click', event => {
        const option = event.target.closest('[data-member-option], [data-member-new]');
        if (!option) return;
        if (option.hasAttribute('data-member-new')) {
            mList.hidden = true;
            if (mId.value) memberClear();
            if (newMember) newMember.hidden = false;
            newMember?.querySelector('input')?.focus();
        } else memberSelect(option.getAttribute('data-member-option'));
    });
    receive.querySelector('[data-member-clear]')?.addEventListener('click', memberClear);
    document.addEventListener('click', event => {
        if (mList && !mList.hidden && !event.target.closest('[data-member-search], [data-member-list]')) mList.hidden = true;
    });
    if (mId?.value) {
        const member = members.find(x => String(x.id) === mId.value);
        if (member) {
            mName.textContent = `${member.name}${member.dni ? ` · DNI ${member.dni}` : ''}`;
            mChip.hidden = false;
            mSearch.disabled = true;
        } else memberClear();
    }

    /* --- Filas de bienes --- */
    const container = receive.querySelector('[data-goods-container]');
    const unitOptions = ['kg', 'litro', 'unidad', 'g', 'lata', 'bolsa', 'paquete', 'caja'];
    const goodsRowHtml = index => `<div class="row g-2 mb-3 panel goods-row" data-goods-row>
<input type="hidden" name="Items.Index" value="${index}" />
<div class="col-md-6 position-relative"><label class="form-label">Bien</label>
<input type="hidden" name="Items[${index}].ProductId" class="js-product-id" />
<input type="text" class="form-control" data-product-search placeholder="Escribe arroz, jabón… y elige o crea" autocomplete="off" />
<div class="list-group position-absolute shadow" data-product-list hidden style="z-index:10;width:100%"></div>
<div class="mt-1 small" data-product-chip hidden>✓ <span data-product-name class="fw-semibold"></span> <button type="button" class="btn btn-sm btn-link p-0" data-product-clear>cambiar</button></div>
<div class="mt-2 small text-warning d-none" data-product-tip></div>
<div class="mt-2 d-none" data-new-product-block>
<div class="input-group input-group-sm mb-1"><span class="input-group-text">Nombre</span><input name="Items[${index}].NewProductName" class="form-control form-control-sm" data-new-product-name autocomplete="off" /></div>
<div class="d-flex gap-2 align-items-center mb-1"><select class="form-select form-select-sm" data-new-unit-select style="width:auto"><option value="">Unidad…</option>${unitOptions.map(u => `<option>${u}</option>`).join('')}<option value="otro">otro…</option></select><input name="Items[${index}].NewProductUnit" class="form-control form-control-sm d-none" style="width:140px" data-new-unit-text placeholder="kg / lata / bolsa" maxlength="40" autocomplete="off" /></div>
</div>
</div>
<div class="col-md-3"><label class="form-label">Cantidad</label><input name="Items[${index}].Quantity" data-qty class="form-control" type="number" min="0.01" step="0.01" /></div>
<div class="col-12 text-end"><button type="button" class="btn btn-sm btn-outline-danger" data-goods-remove>Quitar fila</button></div>
</div>`;

    function setupGoodsRow(row) {
        let typed = '';
        let creating = false;
        const search = row.querySelector('[data-product-search]');
        const list = row.querySelector('[data-product-list]');
        const idInput = row.querySelector('.js-product-id');
        const chip = row.querySelector('[data-product-chip]');
        const chipName = row.querySelector('[data-product-name]');
        const tip = row.querySelector('[data-product-tip]');
        const createBlock = row.querySelector('[data-new-product-block]');
        const nameInput = row.querySelector('[data-new-product-name]');
        const unitSelect = row.querySelector('[data-new-unit-select]');
        const unitText = row.querySelector('[data-new-unit-text]');
        const qty = row.querySelector('[data-qty]');

        const setTip = text => { tip.textContent = text || ''; tip.classList.toggle('d-none', !text); };
        const productSelect = product => {
            idInput.value = product.id;
            chipName.textContent = `${product.name} · ${product.unit}`;
            chip.hidden = false;
            search.disabled = true;
            list.hidden = true;
            hideCreate();
        };
        const productClear = () => {
            idInput.value = '';
            chip.hidden = true;
            search.disabled = false;
            search.focus();
            hideCreate();
        };
        const showCreate = (name, suggestion) => {
            creating = true;
            idInput.value = '';
            chip.hidden = true;
            search.disabled = false;
            createBlock.classList.remove('d-none');
            nameInput.value = nameInput.value || name;
            if (suggestion) setTip(suggestion);
            else setTip(`Se creará «${nameInput.value.trim()}» con la unidad que elijas.`);
            unitSelect.focus();
        };
        const hideCreate = () => {
            creating = false;
            createBlock.classList.add('d-none');
            setTip('');
        };
        /* Productos ya elegidos en otras filas de esta misma recepción. */
        const duplicateRow = () => Array.from(container.querySelectorAll('[data-goods-row]'))
            .find(other => other !== row && other.querySelector('.js-product-id').value === idInput.value
                && other.querySelector('.js-product-id').value !== '');

        search.addEventListener('input', () => {
            if (idInput.value) { idInput.value = ''; chip.hidden = true; search.disabled = false; hideCreate(); }
            typed = search.value.trim();
            if (!typed) { list.hidden = true; return; }
            const q = norm(typed);
            const matches = products.filter(product => norm(product.name).includes(q)).slice(0, 8);
            list.innerHTML = matches.map(product =>
                `<button type="button" class="list-group-item list-group-item-action py-1" data-product-option="${product.id}">${product.name} · ${product.unit}</button>`).join('')
                + `<button type="button" class="list-group-item list-group-item-action py-1 text-primary" data-product-create>➕ Crear «${typed}» como producto nuevo</button>`;
            list.hidden = false;
        });
        list.addEventListener('click', event => {
            const option = event.target.closest('[data-product-option], [data-product-create]');
            if (!option) return;
            list.hidden = true;
            if (option.hasAttribute('data-product-create')) {
                const name = typed;
                const suggestion = products.find(product => similar(product.name, name) && norm(product.name) !== norm(name));
                if (suggestion) {
                    setTip(`¿Quizás querías «${suggestion.name} · ${suggestion.unit}»? `);
                    const use = document.createElement('button');
                    use.type = 'button'; use.className = 'btn btn-sm btn-link p-0'; use.textContent = 'Usar ese';
                    use.addEventListener('click', () => { setTip(''); productSelect(suggestion); });
                    const anyway = document.createElement('button');
                    anyway.type = 'button'; anyway.className = 'btn btn-sm btn-link p-0'; anyway.textContent = 'Crear de todas formas';
                    anyway.addEventListener('click', () => { setTip(''); showCreate(name); });
                    tip.append(use, ' · ', anyway);
                    tip.classList.remove('d-none');
                } else showCreate(name);
            } else {
                const product = products.find(x => String(x.id) === option.getAttribute('data-product-option'));
                const duplicated = Array.from(container.querySelectorAll('[data-goods-row]'))
                    .find(other => other !== row && other.querySelector('.js-product-id').value === String(product.id));
                if (duplicated) {
                    setTip(`«${product.name}» ya está en esta recepción: ajusta la cantidad allá.`);
                    search.value = '';
                    duplicated.querySelector('[data-qty]')?.focus();
                    duplicated.scrollIntoView({ behavior: 'smooth', block: 'center' });
                } else productSelect(product);
            }
        });
        search.addEventListener('blur', () => setTimeout(() => { list.hidden = true; }, 150));
        row.querySelector('[data-product-clear]')?.addEventListener('click', productClear);
        unitSelect.addEventListener('change', () => {
            if (unitSelect.value === 'otro') {
                unitText.classList.remove('d-none');
                unitText.value = '';
                unitText.focus();
            } else {
                unitText.classList.remove('d-none');
                unitText.value = unitSelect.value;
                unitText.classList.add('d-none');
            }
            unitSelect.blur();
        });
        row.querySelector('[data-goods-remove]')?.addEventListener('click', () => row.remove());
        if (idInput.value) {
            const product = products.find(x => String(x.id) === idInput.value);
            if (product) productSelect(product);
        }
    }

    receive.querySelectorAll('[data-goods-row]').forEach(setupGoodsRow);
    receive.querySelector('[data-add-goods]')?.addEventListener('click', () => {
        const rows = container.querySelectorAll('[data-goods-row]');
        if (!container || rows.length >= 50) return;
        const next = 1 + Math.max(-1, ...Array.from(container.querySelectorAll('[name="Items.Index"]')).map(x => Number(x.value)));
        container.insertAdjacentHTML('beforeend', goodsRowHtml(next));
        setupGoodsRow(container.lastElementChild);
        container.lastElementChild.querySelector('[data-product-search]').focus();
    });
}

document.querySelectorAll('.workspace-sidebar a').forEach(link => { if (new URL(link.href).pathname === location.pathname) { link.classList.add("is-active"); link.setAttribute("aria-current", "page"); } });
document.querySelectorAll('[data-copy]').forEach(button => button.addEventListener('click', async () => {
    const target = document.getElementById(button.dataset.copy);
    if (!target) return;
    try { await navigator.clipboard.writeText(target.textContent.trim()); button.textContent = '¡Copiado!'; }
    catch { const r = document.createRange(); r.selectNode(target); window.getSelection().removeAllRanges(); window.getSelection().addRange(r); document.execCommand('copy'); button.textContent = '¡Copiado!'; }
    setTimeout(() => { button.textContent = 'Copiar número'; }, 2000);
}));

/* ===== Almacén: modal para unificar productos mal escritos ===== */
const unifyModal = document.querySelector('[data-unify-modal]');
if (unifyModal) {
    const search = unifyModal.querySelector('[data-unify-search]');
    const target = unifyModal.querySelector('[data-unify-target]');
    const nameInput = unifyModal.querySelector('[data-unify-name]');
    const items = Array.from(unifyModal.querySelectorAll('[data-unify-item]'));
    const norm = text => (text || '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    search?.addEventListener('input', () => {
        const q = norm(search.value.trim());
        items.forEach(item => {
            const match = !q || norm(item.getAttribute('data-unify-item')).includes(q);
            item.classList.toggle('d-none', !match);
        });
    });
    target?.addEventListener('change', () => {
        const opt = target.selectedOptions[0];
        const name = opt?.getAttribute('data-name') || '';
        if (name && !nameInput.value.trim()) nameInput.value = name;
    });
    unifyModal.querySelectorAll('[data-unify-check]').forEach(chk => {
        chk.addEventListener('change', () => {
            if (chk.checked && !nameInput.value.trim()) nameInput.value = chk.getAttribute('data-name') || '';
        });
    });
}
