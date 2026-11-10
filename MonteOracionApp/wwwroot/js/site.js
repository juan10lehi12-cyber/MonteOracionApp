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

    /* --- Filas de bienes (Recepción express sin fricción) --- */
    const container = receive.querySelector('[data-goods-container]');
    const goodsRowHtml = index => `<div class="row g-2 mb-3 panel p-3 border goods-row" data-goods-row>
        <input type="hidden" name="Items.Index" value="${index}" />
        <div class="col-md-10">
            <label class="form-label fw-semibold">¿Qué entregó el donante? (Descripción del bien o bulto)</label>
            <input name="Items[${index}].Description" class="form-control form-control-lg" placeholder="Ej: 1 saco de arroz, 1 caja con víveres variados, 20 sillas, Guitarra..." required autocomplete="off" />
        </div>
        <div class="col-md-2 d-flex align-items-end">
            <button type="button" class="btn btn-outline-danger w-100" data-goods-remove>✕ Quitar</button>
        </div>
    </div>`;

    function setupGoodsRow(row) {
        row.querySelector('[data-goods-remove]')?.addEventListener('click', () => {
            if (container.querySelectorAll('[data-goods-row]').length > 1) {
                row.remove();
            } else {
                alert('Debe haber al menos un ítem o bulto en la recepción.');
            }
        });
    }

    receive.querySelectorAll('[data-goods-row]').forEach(setupGoodsRow);
    receive.querySelector('[data-add-goods]')?.addEventListener('click', () => {
        const rows = container.querySelectorAll('[data-goods-row]');
        if (!container || rows.length >= 50) return;
        const next = 1 + Math.max(-1, ...Array.from(container.querySelectorAll('[name="Items.Index"]')).map(x => Number(x.value)));
        container.insertAdjacentHTML('beforeend', goodsRowHtml(next));
        setupGoodsRow(container.lastElementChild);
        container.lastElementChild.querySelector('input[name$=".Description"]')?.focus();
    });
}

document.querySelectorAll('.workspace-sidebar a').forEach(link => {
    const linkPath = new URL(link.href).pathname;
    if (linkPath === location.pathname || (linkPath !== '/Dashboard' && location.pathname.startsWith(linkPath + '/'))) {
        link.classList.add("is-active");
        link.setAttribute("aria-current", "page");
    }
});
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


/* ===== UX Público: Navegación directa, Desplazamiento suave a anclas y Pestañas de Pago ===== */
(function () {
    // Desplazamiento suave para anclas directas (#donar, #visitanos, #contacto, #donar-viveres, etc.)
    function scrollToHash(hash) {
        if (!hash) return;
        const targetId = hash.replace('#', '').toLowerCase();
        let targetEl = document.getElementById(targetId);
        // Aliases para compatibilidad con anclas de horarios y contacto
        if (!targetEl && (targetId === 'contacto' || targetId === 'horarios')) {
            targetEl = document.getElementById('visitanos');
        }
        if (targetEl) {
            setTimeout(() => {
                targetEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }, 80);
        }
    }

    if (location.hash) {
        scrollToHash(location.hash);
    }
    window.addEventListener('hashchange', () => scrollToHash(location.hash));

    // Si el usuario hace clic en un enlace a una ancla dentro de la página, cierra el menú móvil si está abierto
    document.querySelectorAll('a[href*="#"]').forEach(anchor => {
        anchor.addEventListener('click', e => {
            const hash = anchor.hash;
            if (hash && (anchor.pathname === location.pathname || !anchor.pathname)) {
                scrollToHash(hash);
                const collapseEl = document.getElementById('navigation');
                if (collapseEl?.classList.contains('show') && window.bootstrap?.Collapse) {
                    bootstrap.Collapse.getOrCreateInstance(collapseEl).hide();
                }
            }
        });
    });

    // Pestañas interactivas de Medios de Pago (Yape / Plin / BCP)
    document.querySelectorAll('[data-payment-tab]').forEach(btn => {
        btn.addEventListener('click', () => {
            const targetId = btn.getAttribute('data-payment-tab');
            const nav = btn.closest('.payment-tabs-nav');
            const container = btn.closest('.panel') || document;
            nav?.querySelectorAll('[data-payment-tab]').forEach(b => b.classList.toggle('is-active', b === btn));
            container.querySelectorAll('.payment-tab-pane').forEach(pane => {
                pane.classList.toggle('is-active', pane.id === targetId);
            });
            const payId = btn.getAttribute('data-payment-id');
            const select = document.getElementById('id_medio_pago');
            if (payId && select) select.value = payId;
        });
    });

    // Botones de monto rápido en formulario de donación
    document.querySelectorAll('[data-quick-amount]').forEach(btn => {
        btn.addEventListener('click', () => {
            const montoInput = document.getElementById('monto');
            if (montoInput) {
                montoInput.value = btn.getAttribute('data-quick-amount');
                montoInput.focus();
            }
        });
    });

    // Modal automático del próximo evento destacado al abrir Inicio o Eventos
    const spotlightEl = document.getElementById('featured-event-spotlight');
    if (spotlightEl && window.bootstrap?.Modal) {
        const initialHash = (location.hash || '').replace('#', '');
        const params = new URLSearchParams(location.search);
        if (!initialHash && !params.has('period') && !params.has('page')) {
            const modal = new bootstrap.Modal(spotlightEl);
            setTimeout(() => modal.show(), 250);
        }
    }

    // ===== Control Inteligente, Pausa y Accesibilidad del Hero Carousel =====
    const heroCarouselEl = document.getElementById('heroCarousel');
    if (heroCarouselEl && window.bootstrap?.Carousel) {
        const carousel = bootstrap.Carousel.getOrCreateInstance(heroCarouselEl, {
            interval: 5500,
            pause: 'hover',
            touch: true,
            keyboard: true
        });

        let userPaused = false;
        const pauseBtn = document.getElementById('heroCarouselPause');
        const pauseIcon = pauseBtn?.querySelector('.inst-pause-icon');

        function setPausedState(isPaused, byUser = false) {
            if (byUser) userPaused = isPaused;
            heroCarouselEl.classList.toggle('is-paused', isPaused);
            if (isPaused) {
                carousel.pause();
                if (pauseBtn) {
                    pauseBtn.setAttribute('title', 'Reanudar rotación automática');
                    pauseBtn.setAttribute('aria-label', 'Reanudar rotación automática');
                    if (pauseIcon) pauseIcon.textContent = '▶';
                }
            } else if (!userPaused) {
                carousel.cycle();
                if (pauseBtn) {
                    pauseBtn.setAttribute('title', 'Pausar rotación automática');
                    pauseBtn.setAttribute('aria-label', 'Pausar rotación automática');
                    if (pauseIcon) pauseIcon.textContent = '⏸';
                }
            }
        }

        // Reinicio visual de la barra de progreso al cambiar de diapositiva
        heroCarouselEl.addEventListener('slide.bs.carousel', () => {
            const indicators = heroCarouselEl.querySelectorAll('.inst-indicator-btn');
            indicators.forEach(btn => {
                const fill = btn.querySelector('.inst-indicator-fill');
                if (fill) {
                    fill.style.animation = 'none';
                    void fill.offsetWidth; // Trigger reflow para reiniciar la animación CSS
                    fill.style.animation = '';
                }
            });
        });

        // Botón interactivo de pausa / reanudación
        pauseBtn?.addEventListener('click', e => {
            e.preventDefault();
            const willPause = !userPaused;
            setPausedState(willPause, true);
        });

        // Pausa al pasar el cursor o hacer foco con teclado (WCAG 2.2.2)
        heroCarouselEl.addEventListener('mouseenter', () => {
            if (!userPaused) heroCarouselEl.classList.add('is-paused');
        });
        heroCarouselEl.addEventListener('mouseleave', () => {
            if (!userPaused) heroCarouselEl.classList.remove('is-paused');
        });
        heroCarouselEl.addEventListener('focusin', () => {
            if (!userPaused) heroCarouselEl.classList.add('is-paused');
        });
        heroCarouselEl.addEventListener('focusout', () => {
            if (!userPaused) heroCarouselEl.classList.remove('is-paused');
        });

        // Pausar si la pestaña del navegador está oculta (Page Visibility API)
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                carousel.pause();
                heroCarouselEl.classList.add('is-paused');
            } else if (!userPaused) {
                heroCarouselEl.classList.remove('is-paused');
                carousel.cycle();
            }
        });

        // Respeto a usuarios con "movimiento reducido" (WCAG)
        if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            setPausedState(true, true);
        }
    }

    // Pestañas de la página /Eventos (Próximos, Avisos, Recuerdos con soporte de Hash URL)
    const eventTabs = Array.from(document.querySelectorAll('[data-event-tab]'));
    if (eventTabs.length > 0) {
        function activateEventTab(tabId, updateUrl) {
            eventTabs.forEach(btn => btn.classList.toggle('is-active', btn.getAttribute('data-event-tab') === tabId));
            document.querySelectorAll('.event-tab-pane').forEach(pane => pane.classList.toggle('is-active', pane.id === tabId));
            if (updateUrl) {
                const shortName = tabId.replace('tab-', '');
                history.replaceState(null, '', `${location.pathname}#${shortName}`);
            }
        }

        eventTabs.forEach(btn => {
            btn.addEventListener('click', () => {
                activateEventTab(btn.getAttribute('data-event-tab'), true);
            });
        });

        function resolveTabFromUrl() {
            const hashOrParam = (location.hash || '').replace('#', '') || new URLSearchParams(location.search).get('tab');
            if (hashOrParam === 'finalizados' || hashOrParam === 'proximos' || hashOrParam === 'calendario') {
                activateEventTab(`tab-${hashOrParam}`, false);
            }
        }

        resolveTabFromUrl();
        window.addEventListener('hashchange', resolveTabFromUrl);
    }
})();
