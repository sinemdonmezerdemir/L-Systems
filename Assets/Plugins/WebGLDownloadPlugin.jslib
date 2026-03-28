mergeInto(LibraryManager.library, {
    DownloadImage: function(filenamePtr, base64Ptr) {
        // C#'tan belleğe yazılan pointer'ları JavaScript string'lerine çeviriyoruz.
        var filename = UTF8ToString(filenamePtr);
        var base64 = UTF8ToString(base64Ptr);

        // Tarayıcıya indirme işlemini yaptırmak için sanal bir <a> (link) DOM elemanı oluşturuyoruz.
        var element = document.createElement('a');
        element.setAttribute('href', 'data:image/png;base64,' + base64);
        element.setAttribute('download', filename);

        // Elemanı geçici olarak sayfaya ekle, tıklama event'ini tetikle ve DOM'dan temizle.
        element.style.display = 'none';
        document.body.appendChild(element);
        element.click();
        document.body.removeChild(element);
    }
});