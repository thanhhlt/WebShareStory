(function () {
    var gtagScript = document.createElement('script');
    gtagScript.async = true;
    gtagScript.src = "https://www.googletagmanager.com/gtag/js?id=G-1PHCN6YN6R";
    document.head.appendChild(gtagScript);

    window.dataLayer = window.dataLayer || [];
    function gtag() { dataLayer.push(arguments); }
    gtag('js', new Date());
    gtag('config', 'G-1PHCN6YN6R', {
        'anonymize_ip': true,
        'cookie_flags': 'Secure;SameSite=Lax',
        'cookie_expires': 63072000,
        'send_page_view': true
    });
})();