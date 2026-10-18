document.querySelectorAll('form[data-confirm]').forEach(form => form.addEventListener('submit', event => {
    if (!window.confirm(form.dataset.confirm)) event.preventDefault();
}));
const kind = document.querySelector('[data-donation-kind]');
const donor = document.querySelector('[data-donor-kind]');
function toggleFields(select, attribute) {
    if (!select) return;
    document.querySelectorAll(`[${attribute}]`).forEach(section => {
        const visible = section.getAttribute(attribute) === select.value;
        section.hidden = !visible;
        section.querySelectorAll('input,select,textarea').forEach(field => field.disabled = !visible);
    });
}
if (kind) { kind.addEventListener('change', () => toggleFields(kind, 'data-kind')); toggleFields(kind, 'data-kind'); }
if (donor) { donor.addEventListener('change', () => toggleFields(donor, 'data-donor')); toggleFields(donor, 'data-donor'); }
document.getElementById('add-item')?.addEventListener('click', () => {
    const container = document.getElementById('goods-items');
    const index = container.querySelectorAll('.goods-row').length;
    if (index >= 50) return;
    const row = document.createElement('div'); row.className = 'row g-2 mb-3 goods-row';
    [['Description', 'Descripción', 'text', '6'], ['Quantity', 'Cantidad', 'number', '3'], ['Unit', 'Unidad', 'text', '3']].forEach(([key, title, type, width]) => {
        const column = document.createElement('div'); column.className = `col-md-${width}`;
        const label = document.createElement('label'); label.textContent = title; label.htmlFor = `Items_${index}__${key}`;
        const input = document.createElement('input'); input.name = `Items[${index}].${key}`; input.id = label.htmlFor; input.type = type; input.className = 'form-control';
        if(type === 'number') { input.min = '0.01'; input.step = '0.01'; } else { input.maxLength = key === 'Description' ? 250 : 40; }
        column.append(label, input); row.append(column);
    });
    container.append(row);
});
