/**
 * meeting-booking.js
 * Place in: wwwroot/js/meeting-booking.js
 * Include AFTER the partial view in _Layout.cshtml or the page:
 *     <script src="~/js/meeting-booking.js"></script>
 */

const MeetingBooking = (() => {

    /* ──────────── STATE ──────────── */
    const state = {
        phone: '',
        name: '',
        email: '',
        selectedDate: null,   // Date object
        selectedTime: null,   // "10:00 AM"
        currentYear: null,
        currentMonth: null,   // 0-based
        meetLink: ''
    };

    /* ──────────── CONFIG ──────────────────────────────────────────
       Customize: set organizer name, booking endpoint,
       available days (0=Sun … 6=Sat), earliest/latest hour.
    ─────────────────────────────────────────────────────────────── */
    const CONFIG = {
        organizerName:    'Ronak Malu',     // shown in Step 2 info panel
        organizerInitial: 'R',              // avatar letter
        durationMin:      15,               // meeting duration
        slotInterval:     15,               // calendar slot every N minutes
        startHour:        10,               // 10:00 AM first slot
        endHour:          19,               // last slot starts before 7 PM
        availableDays:    [1,2,3,4,5],      // Mon-Fri (0=Sun, 6=Sat)
        advanceDays:      1,                // can't book same-day (0 = can)
        maxFutureDays:    60,               // how far ahead to allow
        bookingEndpoint:  '/Meeting/Book',  // your ASP.NET controller action
    };

    /* ──────────── OPEN / CLOSE ──────────── */
    function open() {
        const overlay = document.getElementById('meetingBookingOverlay');
        overlay.style.display = 'flex';
        document.body.style.overflow = 'hidden';
        goToStep(1);
        document.getElementById('mb-organizer-name').textContent = CONFIG.organizerName;
        document.querySelector('.mb-avatar').textContent = CONFIG.organizerInitial;

        const now = new Date();
        state.currentYear  = now.getFullYear();
        state.currentMonth = now.getMonth();
        renderCalendar();
    }

    function close() {
        document.getElementById('meetingBookingOverlay').style.display = 'none';
        document.body.style.overflow = '';
        resetState();
    }

    function resetState() {
        state.phone = '';
        state.name  = '';
        state.email = '';
        state.selectedDate = null;
        state.selectedTime = null;
        state.meetLink = '';
        document.getElementById('mb-phone').value = '';
        document.getElementById('mb-name').value  = '';
        document.getElementById('mb-email').value = '';
        document.getElementById('mb-phone-error').style.display = 'none';
        document.getElementById('mb-email-error').style.display = 'none';
        document.getElementById('mb-time-panel').style.display  = 'none';
    }

    /* ──────────── STEP NAVIGATION ──────────── */
    function goToStep(n) {
        [1, 2, 3, 4].forEach(i => {
            const el = document.getElementById('mb-step' + i);
            if (el) el.style.display = (i === n) ? '' : 'none';
        });
    }

    /* ──────────── STEP 1: PHONE ──────────── */
    function continueToCalendar() {
        const input = document.getElementById('mb-phone');
        const err   = document.getElementById('mb-phone-error');
        const val   = input.value.replace(/\D/g, '');

        if (val.length !== 10) {
            err.style.display = 'block';
            input.focus();
            return;
        }
        err.style.display = 'none';
        state.phone = '+91' + val;
        goToStep(2);
        renderCalendar();
    }

    /* ──────────── CALENDAR ──────────── */
    function prevMonth() {
        state.currentMonth--;
        if (state.currentMonth < 0) {
            state.currentMonth = 11;
            state.currentYear--;
        }
        state.selectedDate = null;
        state.selectedTime = null;
        document.getElementById('mb-time-panel').style.display = 'none';
        renderCalendar();
    }

    function nextMonth() {
        state.currentMonth++;
        if (state.currentMonth > 11) {
            state.currentMonth = 0;
            state.currentYear++;
        }
        state.selectedDate = null;
        state.selectedTime = null;
        document.getElementById('mb-time-panel').style.display = 'none';
        renderCalendar();
    }

    function renderCalendar() {
        const MONTH_NAMES = [
            'January','February','March','April','May','June',
            'July','August','September','October','November','December'
        ];

        const y = state.currentYear;
        const m = state.currentMonth;

        document.getElementById('mb-month-label').textContent = `${MONTH_NAMES[m]} ${y}`;

        const grid   = document.getElementById('mb-calendar-grid');
        const today  = new Date();
        today.setHours(0, 0, 0, 0);

        // Earliest bookable day
        const earliest = new Date(today);
        earliest.setDate(today.getDate() + CONFIG.advanceDays);

        // Latest bookable day
        const latest = new Date(today);
        latest.setDate(today.getDate() + CONFIG.maxFutureDays);

        // First day of month & total days
        const firstDOW  = new Date(y, m, 1).getDay();   // 0=Sun
        const daysInMon = new Date(y, m + 1, 0).getDate();

        grid.innerHTML = '';

        // Empty cells before day 1
        for (let i = 0; i < firstDOW; i++) {
            const blank = document.createElement('div');
            grid.appendChild(blank);
        }

        for (let d = 1; d <= daysInMon; d++) {
            const date   = new Date(y, m, d);
            const btn    = document.createElement('button');
            btn.type = 'button';
            btn.textContent = d;
            btn.className = 'mb-day';

            const isToday     = date.getTime() === today.getTime();
            const isPast      = date < earliest;
            const isTooFar    = date > latest;
            const isAvailable = CONFIG.availableDays.includes(date.getDay());
            const isSelected  = state.selectedDate && date.getTime() === state.selectedDate.getTime();

            if (isToday)    btn.classList.add('today');
            if (isSelected) btn.classList.add('selected');

            if (isPast || isTooFar || !isAvailable) {
                btn.classList.add(isPast || isTooFar ? 'past' : 'unavailable');
                btn.setAttribute('aria-disabled', 'true');
            } else {
                btn.classList.add('available');
                btn.setAttribute('aria-label', date.toDateString());
                btn.addEventListener('click', () => selectDate(date, btn));
            }

            grid.appendChild(btn);
        }
    }

    function selectDate(date, btn) {
        // Deselect previous
        document.querySelectorAll('.mb-day.selected').forEach(b => b.classList.remove('selected'));
        btn.classList.add('selected');
        state.selectedDate = date;
        state.selectedTime = null;
        renderTimeSlots(date);
    }

    /* ──────────── TIME SLOTS ──────────── */
    function renderTimeSlots(date) {
        const panel  = document.getElementById('mb-time-panel');
        const header = document.getElementById('mb-time-date-label');
        const slots  = document.getElementById('mb-time-slots');

        const DAY_NAMES  = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'];
        const MONTH_NAMES= ['January','February','March','April','May','June','July','August','September','October','November','December'];

        header.textContent = `${DAY_NAMES[date.getDay()]}, ${MONTH_NAMES[date.getMonth()]} ${date.getDate()}`;
        slots.innerHTML = '';

        const { startHour, endHour, slotInterval } = CONFIG;

        for (let h = startHour; h < endHour; h++) {
            for (let min = 0; min < 60; min += slotInterval) {
                const label = formatTime(h, min);
                const btn   = document.createElement('button');
                btn.type = 'button';
                btn.className = 'mb-slot';
                btn.textContent = label;
                btn.addEventListener('click', () => selectTime(label, btn));
                slots.appendChild(btn);
            }
        }

        panel.style.display = '';
        panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function selectTime(label, btn) {
        document.querySelectorAll('.mb-slot.selected').forEach(b => b.classList.remove('selected'));
        btn.classList.add('selected');
        state.selectedTime = label;

        // Auto-advance to step 3 after brief delay
        setTimeout(() => {
            if (state.selectedTime) showConfirm();
        }, 280);
    }

    function formatTime(h, min) {
        const suffix = h < 12 ? 'AM' : 'PM';
        const hh     = h % 12 === 0 ? 12 : h % 12;
        const mm     = min === 0 ? '00' : min;
        return `${hh}:${mm} ${suffix}`;
    }

    /* ──────────── STEP 3: CONFIRM ──────────── */
    function showConfirm() {
        if (!state.selectedDate || !state.selectedTime) return;

        const DAY_NAMES   = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'];
        const MONTH_NAMES = ['January','February','March','April','May','June','July','August','September','October','November','December'];

        const d   = state.selectedDate;
        const dtStr = `${DAY_NAMES[d.getDay()]}, ${d.getDate()} ${MONTH_NAMES[d.getMonth()]} ${d.getFullYear()} at ${state.selectedTime} (IST)`;

        document.getElementById('mb-confirm-datetime').textContent = dtStr;
        document.getElementById('mb-confirm-phone').textContent    = state.phone;
        goToStep(3);
    }

    /* ──────────── STEP 4: SUBMIT ──────────── */
    async function submitBooking() {
        const nameEl  = document.getElementById('mb-name');
        const emailEl = document.getElementById('mb-email');
        const errEl   = document.getElementById('mb-email-error');
        const btnText = document.getElementById('mb-submit-text');
        const btnLoad = document.getElementById('mb-submit-loader');

        state.name  = nameEl.value.trim();
        state.email = emailEl.value.trim();

        // Validate
        if (!state.email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(state.email)) {
            errEl.style.display = 'block';
            emailEl.focus();
            return;
        }
        errEl.style.display = 'none';

        // Loading state
        btnText.style.display = 'none';
        btnLoad.style.display = '';

        const d   = state.selectedDate;
        const DAY_NAMES   = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'];
        const MONTH_NAMES = ['January','February','March','April','May','June','July','August','September','October','November','December'];
        const dtStr = `${DAY_NAMES[d.getDay()]}, ${d.getDate()} ${MONTH_NAMES[d.getMonth()]} ${d.getFullYear()} at ${state.selectedTime} (IST)`;

        const payload = {
            name:         state.name,
            email:        state.email,
            phone:        state.phone,
            date:         `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`,
            time:         state.selectedTime,
            durationMin:  CONFIG.durationMin,
            displayDatetime: dtStr
        };

        try {
            const res = await fetch(CONFIG.bookingEndpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify(payload)
            });

            if (!res.ok) throw new Error('Server error ' + res.status);

            const data = await res.json();

            // Show success step
            document.getElementById('mb-done-datetime').textContent = dtStr;

            if (data.meetLink) {
                state.meetLink = data.meetLink;
                const linkBox  = document.getElementById('mb-meet-link-box');
                const linkEl   = document.getElementById('mb-meet-link');
                linkEl.href        = data.meetLink;
                linkEl.textContent = data.meetLink;
                linkBox.style.display = '';
            }

            goToStep(4);

        } catch (err) {
            console.error('Booking error:', err);
            alert('Something went wrong. Please try again or call us directly.');
        } finally {
            btnText.style.display = '';
            btnLoad.style.display = 'none';
        }
    }

    /* ──────────── HELPERS ──────────── */
    function getAntiForgeryToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    // Close on overlay click
    document.addEventListener('DOMContentLoaded', () => {
        const overlay = document.getElementById('meetingBookingOverlay');
        if (overlay) {
            overlay.addEventListener('click', (e) => {
                if (e.target === overlay) close();
            });
        }
        // Close on Escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') close();
        });
        // Only allow digits in phone input
        const phoneInput = document.getElementById('mb-phone');
        if (phoneInput) {
            phoneInput.addEventListener('input', () => {
                phoneInput.value = phoneInput.value.replace(/\D/g, '').slice(0, 10);
            });
            phoneInput.addEventListener('keydown', (e) => {
                if (e.key === 'Enter') continueToCalendar();
            });
        }
    });

    /* ──────────── PUBLIC API ──────────── */
    return { open, close, continueToCalendar, prevMonth, nextMonth, goToStep, submitBooking };

})();