/* SyncTally – site.js
   Theme toggle, hamburger, mega menu, journey tabs, pricing toggle, toasts,
   modal helper and the "Chat on WhatsApp" dialog (AJAX + anti-forgery header). */
(function () {
  'use strict';

  var $ = function (s, r) { return (r || document).querySelector(s); };
  var $$ = function (s, r) { return Array.prototype.slice.call((r || document).querySelectorAll(s)); };
  var THEME_KEY = 'synctally_theme';

  /* ---------------- Toasts ---------------- */
  var Toast = {
    show: function (msg, type, ms) {
      var root = $('#toast-root'); if (!root) return;
      var el = document.createElement('div');
      el.className = 'toast ' + (type || 'success');
      el.setAttribute('role', type === 'error' ? 'alert' : 'status');
      el.textContent = msg;
      root.appendChild(el);
      setTimeout(function () { el.remove(); }, ms || 3600);
    }
  };
  window.Toast = Toast;

  /* ---------------- Theme ---------------- */
  function currentTheme() { return document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light'; }
  function paintThemeButton() {
    var dark = currentTheme() === 'dark';
    $$('[data-act="theme"]').forEach(function (b) {
      b.textContent = dark ? '☀️' : '🌙';
      b.setAttribute('aria-label', 'Switch to ' + (dark ? 'light' : 'dark') + ' mode');
    });
  }
  function toggleTheme() {
    var next = currentTheme() === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', next);
    try { localStorage.setItem(THEME_KEY, next); } catch (e) { /* storage blocked */ }
    paintThemeButton();
  }

  /* ---------------- Mega menu / hamburger ---------------- */
  function closeMega() {
    $$('[data-toggle="mega"]').forEach(function (b) {
      b.setAttribute('aria-expanded', 'false');
      var chev = $('.chev', b); if (chev) { chev.textContent = '⌄'; chev.classList.remove('up'); }
    });
    var p = $('#mega-panel'); if (p) p.hidden = true;
  }
  function toggleMega(btn) {
    var p = $('#mega-panel'); if (!p) return;
    var open = p.hidden;
    p.hidden = !open;
    btn.setAttribute('aria-expanded', String(open));
    var chev = $('.chev', btn); if (chev) { chev.textContent = open ? '⌃' : '⌄'; chev.classList.toggle('up', open); }
  }
  function toggleHamburger(btn) {
    var nav = $('#nav-links'); if (!nav) return;
    var open = !nav.classList.contains('open');
    nav.classList.toggle('open', open);
    btn.setAttribute('aria-expanded', String(open));
    btn.textContent = open ? '✕' : '☰';
  }

  /* ---------------- Modal (focus trap, Esc, backdrop) ---------------- */
  function openModal(title, bodyNode) {
    var prev = document.activeElement;
    var wrap = document.createElement('div');
    wrap.className = 'modal-backdrop';
    var tid = 'mt' + Date.now();
    wrap.innerHTML = '<div class="modal" role="dialog" aria-modal="true" aria-labelledby="' + tid + '">' +
      '<div class="modal-head"><h2 id="' + tid + '"></h2>' +
      '<button type="button" class="icon-btn" data-close aria-label="Close dialog">✕</button></div>' +
      '<div class="modal-body"></div></div>';
    $('h2', wrap).textContent = title;
    $('.modal-body', wrap).appendChild(bodyNode);
    $('#modal-root').appendChild(wrap);

    function close() {
      if (!wrap.isConnected) return;
      wrap.remove();
      document.removeEventListener('keydown', onKey);
      if (prev && prev.focus) prev.focus();
    }
    function onKey(e) {
      if (e.key === 'Escape') { close(); return; }
      if (e.key === 'Tab') {
        var f = $$('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])', wrap).filter(function (x) { return !x.disabled; });
        if (!f.length) return;
        var first = f[0], last = f[f.length - 1];
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
      }
    }
    wrap.addEventListener('mousedown', function (e) { if (e.target === wrap) close(); });
    wrap.addEventListener('click', function (e) { if (e.target.closest('[data-close]')) close(); });
    document.addEventListener('keydown', onKey);
    var firstInput = $('input, select, textarea', wrap); if (firstInput) firstInput.focus();
    return { el: wrap, close: close };
  }

  /* ---------------- WhatsApp chat dialog ---------------- */
  function openChat() {
    var tpl = $('#chat-template'); if (!tpl) return;
    var node = tpl.content.firstElementChild.cloneNode(true);
    var m = openModal('Chat with SyncTally on WhatsApp', node);
    var form = $('form', m.el);
    var send = $('button[type=submit]', form);

    function setErr(name, msg) {
      var e = $('[data-err="' + name + '"]', form); if (e) e.textContent = msg || '';
      var i = form.elements[name]; if (i) i.setAttribute('aria-invalid', msg ? 'true' : 'false');
    }
    function validate() {
      var ok = true;
      var name = form.elements.name.value.trim(), phone = form.elements.phone.value.trim();
      setErr('name', ''); setErr('phone', '');
      if (name.length < 2) { setErr('name', 'Name must be at least 2 characters.'); ok = false; }
      if (!/^[6-9]\d{9}$/.test(phone)) { setErr('phone', 'Enter a valid 10-digit Indian mobile number.'); ok = false; }
      return ok;
    }

    form.addEventListener('submit', function (e) {
      e.preventDefault();
      if (!validate()) return;
      send.disabled = true;
      var body = {
        name: form.elements.name.value.trim(),
        phone: form.elements.phone.value.trim(),
        topic: form.elements.topic.value,
        message: form.elements.msg.value.trim() || null
      };
      var token = ($('#af-token input[name="__RequestVerificationToken"]') || {}).value || '';
      fetch(form.getAttribute('data-url'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token, 'X-Requested-With': 'XMLHttpRequest' },
        credentials: 'same-origin',
        body: JSON.stringify(body)
      }).then(function (r) {
        return r.json().then(function (j) { return { status: r.status, json: j }; });
      }).then(function (res) {
        if (res.json && res.json.ok) {
          window.open(res.json.url, '_blank', 'noopener');
          m.close();
          Toast.show('Opening WhatsApp… we saved your request too.');
          return;
        }
        var errs = (res.json && res.json.errors) || {};
        var shown = false;
        Object.keys(errs).forEach(function (k) {
          var key = k.charAt(0).toLowerCase() + k.slice(1);
          if (form.elements[key]) { setErr(key, errs[k][0]); shown = true; }
        });
        if (!shown) Toast.show((res.json && res.json.message) || 'Could not start the chat. Please try again.', 'error');
        send.disabled = false;
      }).catch(function () {
        Toast.show('Network problem – please try again.', 'error');
        send.disabled = false;
      });
    });
  }

  /* ---------------- Journey tabs (home) ---------------- */
  function initTabs() {
    var tabs = $$('.tab[data-i]'), panel = $('#tabpanel');
    if (!tabs.length || !panel) return;
    function show(i) {
      tabs.forEach(function (t, j) { t.setAttribute('aria-selected', String(j === i)); t.tabIndex = j === i ? 0 : -1; });
      var t = tabs[i];
      panel.setAttribute('aria-labelledby', t.id);
      panel.innerHTML = '';
      var ico = document.createElement('div'); ico.className = 'tab-icon'; ico.setAttribute('aria-hidden', 'true'); ico.textContent = t.getAttribute('data-icon');
      var box = document.createElement('div');
      var h = document.createElement('h3'); h.textContent = (i + 1) + '. ' + t.getAttribute('data-title');
      var p = document.createElement('p'); p.style.margin = '0'; p.textContent = t.getAttribute('data-desc');
      box.appendChild(h); box.appendChild(p); panel.appendChild(ico); panel.appendChild(box);
    }
    tabs.forEach(function (t, i) {
      t.addEventListener('click', function () { show(i); });
      t.addEventListener('keydown', function (e) {
        var n = null;
        if (e.key === 'ArrowRight') n = (i + 1) % tabs.length;
        if (e.key === 'ArrowLeft') n = (i + tabs.length - 1) % tabs.length;
        if (n !== null) { e.preventDefault(); show(n); tabs[n].focus(); }
      });
    });
    show(0);
  }

  /* ---------------- Pricing toggle ---------------- */
  function initPricing() {
    var sw = $('.switch'); if (!sw) return;
    var fmt = new Intl.NumberFormat('en-IN');
    function draw(term) {
      $$('.switch button').forEach(function (b) { b.setAttribute('aria-pressed', String(+b.dataset.term === term)); });
      $$('.plan').forEach(function (card) {
        var y1 = +card.dataset.price1y;
        var per = term === 3 ? Math.round(y1 * 0.8) : y1;
        $('[data-price]', card).textContent = '₹' + fmt.format(per);
        $('[data-billing]', card).textContent = term === 3
          ? 'Billed ₹' + fmt.format(per * 3) + ' every 3 years · save 20%' : 'Billed yearly';
      });
    }
    sw.addEventListener('click', function (e) {
      var b = e.target.closest('button[data-term]'); if (b) draw(+b.dataset.term);
    });
    draw(1);
  }

  /* ---------------- Flash toast from TempData ---------------- */
  function initFlash() {
    var f = $('#flash-toast'); if (!f) return;
    var raw = f.getAttribute('data-message') || '';
    var i = raw.indexOf('|');
    var kind = i > -1 ? raw.slice(0, i) : 'ok';
    var msg = i > -1 ? raw.slice(i + 1) : raw;
    Toast.show(msg, kind === 'error' ? 'error' : kind === 'info' ? 'info' : 'success');
  }

  /* ---------------- Wiring ---------------- */
  document.addEventListener('click', function (e) {
    var t = e.target;
    if (!(t instanceof Element)) return;
    var chat = t.closest('[data-act="wa-chat"]');
    if (chat) { e.preventDefault(); var nav = $('#nav-links'); if (nav) nav.classList.remove('open'); openChat(); return; }
    var th = t.closest('[data-act="theme"]'); if (th) { toggleTheme(); return; }
    var hb = t.closest('[data-act="menu"]'); if (hb) { toggleHamburger(hb); return; }
    var mg = t.closest('[data-toggle="mega"]'); if (mg) { toggleMega(mg); return; }
    if (!t.closest('.has-menu')) closeMega();
  });
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
      var open = $('[data-toggle="mega"][aria-expanded="true"]');
      if (open) { closeMega(); open.focus(); }
    }
  });

  document.addEventListener('DOMContentLoaded', function () {
    paintThemeButton();
    initTabs();
    initPricing();
    initFlash();
    var y = $('#year'); if (y) y.textContent = new Date().getFullYear();
  });
})();

/* Auto-submit filter selects (progressive enhancement – the Search button still works without JS) */
document.addEventListener('change', function (e) {
  var t = e.target;
  if (t instanceof Element && t.matches('select[data-autosubmit]') && t.form) t.form.submit();
});

/* ---------------- Free-trial helpers ---------------- */
(function () {
  'use strict';
  document.addEventListener('click', function (e) {
    var t = e.target;
    if (!(t instanceof Element)) return;
    var tg = t.closest('[data-toggle-password]');
    if (tg) {
      var input = tg.parentElement.querySelector('input');
      var show = input.type === 'password';
      input.type = show ? 'text' : 'password';
      tg.textContent = show ? 'Hide' : 'Show';
      tg.setAttribute('aria-label', (show ? 'Hide' : 'Show') + ' password');
    }
  });

  document.addEventListener('input', function (e) {
    var t = e.target;
    if (t instanceof HTMLInputElement && t.classList.contains('otp-input')) t.value = t.value.replace(/\D/g, '').slice(0, 6);
    if (t instanceof HTMLInputElement && t.name === 'Phone') t.value = t.value.replace(/\D/g, '').slice(0, 10);
  });

  /* Disable the submit button after a valid submit to prevent double posts */
  document.addEventListener('submit', function (e) {
    var f = e.target;
    if (!(f instanceof HTMLFormElement)) return;
    setTimeout(function () {
      if (e.defaultPrevented) return;
      var b = f.querySelector('button.spinner-btn[type=submit]');
      if (b) { b.disabled = true; b.dataset.label = b.textContent; b.textContent = 'Please wait…'; }
    }, 0);
  });

  document.addEventListener('DOMContentLoaded', function () {
    /* Resend countdown */
    var rb = document.querySelector('[data-resend-seconds]');
    if (rb) {
      var left = parseInt(rb.getAttribute('data-resend-seconds'), 10) || 0;
      if (left > 0) {
        var iv = setInterval(function () {
          left--;
          if (left <= 0) { clearInterval(iv); rb.disabled = false; rb.textContent = 'Resend code'; }
          else rb.textContent = 'Resend code in ' + left + 's';
        }, 1000);
      }
    }
    /* Auto-navigate to the application after sign-up */
    var box = document.querySelector('[data-redirect-url]');
    if (box) {
      var secs = parseInt(box.getAttribute('data-redirect-seconds'), 10) || 8;
      var span = box.querySelector('[data-countdown]');
      var url = box.getAttribute('data-redirect-url');
      var t = setInterval(function () {
        secs--;
        if (span) span.textContent = String(Math.max(secs, 0));
        if (secs <= 0) { clearInterval(t); window.location.href = url; }
      }, 1000);
    }
  });
})();

/* ---------- Blog article: reading progress, TOC highlight, copy link ---------- */
(function () {
  var bar = document.getElementById('read-bar');
  var body = document.getElementById('post-body');
  if (bar && body) {
    var links = [].slice.call(document.querySelectorAll('.toc a'));
    var heads = links.map(function (a) { return document.getElementById(a.getAttribute('href').slice(1)); });
    var update = function () {
      var r = body.getBoundingClientRect();
      var total = r.height - window.innerHeight * 0.6;
      var done = Math.min(Math.max(-r.top / (total > 0 ? total : 1), 0), 1);
      bar.style.width = (done * 100).toFixed(1) + '%';
      var cur = -1;
      heads.forEach(function (h, i) { if (h && h.getBoundingClientRect().top < 120) cur = i; });
      links.forEach(function (a, i) { a.classList.toggle('on', i === cur); });
    };
    window.addEventListener('scroll', update, { passive: true });
    window.addEventListener('resize', update);
    update();
  }
  var copy = document.getElementById('copy-link');
  if (copy) {
    copy.addEventListener('click', function () {
      var done = function () { copy.textContent = 'Link copied'; setTimeout(function () { copy.textContent = 'Copy link'; }, 2000); };
      if (navigator.clipboard) { navigator.clipboard.writeText(window.location.href).then(done); }
      else { var t = document.createElement('input'); t.value = window.location.href; document.body.appendChild(t); t.select(); document.execCommand('copy'); t.remove(); done(); }
    });
  }
})();
