window.clubInterop = {
    focusElement: function (selector) {
        const el = document.querySelector(selector);
        if (el) { el.focus(); }
    },
    scrollToTop: function () {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
};
