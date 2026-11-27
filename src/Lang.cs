using System;
using System.Collections.Generic;
using System.Globalization;

namespace Companian
{
    /// <summary>Runtime-switchable UI translations. Column order: tr, en, de, es.</summary>
    internal static class Lang
    {
        public static readonly string[] Codes = { "tr", "en", "de", "es" };
        public static readonly string[] Names = { "Türkçe", "English", "Deutsch", "Español" };

        private static readonly Dictionary<string, string[]> map = new Dictionary<string, string[]>();
        private static int current = 1;

        public static event EventHandler Changed;

        public static string Code
        {
            get { return Codes[current]; }
        }

        public static int Index
        {
            get { return current; }
        }

        public static void Set(string code)
        {
            int i = Array.IndexOf(Codes, code);
            current = i < 0 ? 1 : i;
            EventHandler h = Changed;
            if (h != null) h(null, EventArgs.Empty);
        }

        public static string DetectDefault()
        {
            string c = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return Array.IndexOf(Codes, c) >= 0 ? c : "en";
        }

        public static string T(string key)
        {
            string[] v;
            if (!map.TryGetValue(key, out v)) return key;
            return string.IsNullOrEmpty(v[current]) ? v[1] : v[current];
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        private static void A(string key, string tr, string en, string de, string es)
        {
            map[key] = new[] { tr, en, de, es };
        }

        static Lang()
        {
            // Navigation
            A("nav_clicker", "Otomatik Tıklayıcı", "Auto Clicker", "Auto-Klicker", "Autoclic");
            A("nav_shutdown", "Kapatma Zamanlayıcı", "Shutdown Timer", "Herunterfahr-Timer", "Apagado programado");
            A("nav_awake", "Uyanık Tut", "Keep Awake", "Wach halten", "Mantener activo");
            A("nav_reminder", "Hatırlatıcı", "Reminders", "Erinnerungen", "Recordatorios");
            A("nav_setup", "Toplu Kurulum", "Bulk Install", "Massen-Setup", "Instalación masiva");
            A("nav_settings", "Ayarlar", "Settings", "Einstellungen", "Ajustes");

            // Units
            A("unit_h", "sa", "h", "Std", "h");
            A("unit_m", "dk", "min", "Min", "min");
            A("unit_s", "sn", "s", "s", "s");
            A("unit_ms", "ms", "ms", "ms", "ms");

            // Auto clicker
            A("clk_sub", "Seçtiğin yere, istediğin hızda ve sayıda otomatik tıklar.",
                "Clicks automatically at the spot, speed and count you choose.",
                "Klickt automatisch an der gewählten Stelle, in Tempo und Anzahl nach Wahl.",
                "Hace clic automáticamente en el lugar, ritmo y cantidad que elijas.");
            A("clk_card_pos", "Tıklama konumu", "Click position", "Klickposition", "Posición del clic");
            A("clk_pos_cursor", "İmlecin bulunduğu yer", "Wherever the cursor is", "Aktuelle Mausposition", "Donde esté el cursor");
            A("clk_pos_fixed", "Sabit konum:", "Fixed position:", "Feste Position:", "Posición fija:");
            A("clk_pick", "Konum seç (3 sn)", "Pick position (3 s)", "Position wählen (3 s)", "Elegir posición (3 s)");
            A("clk_picking", "İmleci hedefe götür: {0}...", "Move the cursor to the target: {0}...", "Maus zum Ziel bewegen: {0}...", "Mueve el cursor al objetivo: {0}...");
            A("clk_pick_hint", "İpucu: F8'e basarak imlecin olduğu yeri anında kaydedebilirsin.",
                "Tip: press F8 to save the current cursor position instantly.",
                "Tipp: Mit F8 wird die aktuelle Mausposition sofort gespeichert.",
                "Consejo: pulsa F8 para guardar al instante la posición del cursor.");
            A("clk_live", "İmleç şu an: {0}, {1}", "Cursor now: {0}, {1}", "Maus jetzt: {0}, {1}", "Cursor ahora: {0}, {1}");
            A("clk_card_click", "Tıklama", "Click", "Klick", "Clic");
            A("clk_button", "Fare tuşu:", "Mouse button:", "Maustaste:", "Botón:");
            A("btn_left", "Sol", "Left", "Links", "Izquierdo");
            A("btn_right", "Sağ", "Right", "Rechts", "Derecho");
            A("btn_middle", "Orta", "Middle", "Mitte", "Central");
            A("clk_type", "Tür:", "Type:", "Art:", "Tipo:");
            A("clk_single", "Tek tık", "Single click", "Einfachklick", "Clic simple");
            A("clk_double", "Çift tık", "Double click", "Doppelklick", "Doble clic");
            A("clk_interval", "Her tıklama arası:", "Time between clicks:", "Zeit zwischen Klicks:", "Tiempo entre clics:");
            A("clk_rate", "≈ saniyede {0} tıklama", "≈ {0} clicks per second", "≈ {0} Klicks pro Sekunde", "≈ {0} clics por segundo");
            A("clk_card_repeat", "Ne kadar tıklansın?", "How long to click?", "Wie lange klicken?", "¿Cuánto tiempo?");
            A("clk_rep_count", "Belirli sayıda tıkla:", "Click a set number of times:", "Feste Anzahl Klicks:", "Número fijo de clics:");
            A("clk_rep_duration", "Belirli süre boyunca tıkla (sn):", "Click for a duration (s):", "Für eine Dauer klicken (s):", "Hacer clic durante (s):");
            A("clk_rep_infinite", "Sonsuz (durdurana kadar)", "Forever (until stopped)", "Endlos (bis zum Stopp)", "Infinito (hasta detener)");
            A("clk_delay", "Başlamadan önce bekle (sn):", "Wait before starting (s):", "Vor dem Start warten (s):", "Esperar antes de empezar (s):");
            A("clk_start", "Başlat  (F6)", "Start  (F6)", "Start  (F6)", "Iniciar  (F6)");
            A("clk_stop", "Durdur  (F6)", "Stop  (F6)", "Stopp  (F6)", "Detener  (F6)");
            A("clk_idle", "Hazır. F6 ile her yerden başlatıp durdurabilirsin.", "Ready. Press F6 anywhere to start or stop.",
                "Bereit. F6 startet und stoppt von überall.", "Listo. Pulsa F6 en cualquier lugar para iniciar o detener.");
            A("clk_running", "Çalışıyor: {0} tıklama", "Running: {0} clicks", "Läuft: {0} Klicks", "En marcha: {0} clics");
            A("clk_waiting", "{0} sn sonra başlıyor...", "Starting in {0} s...", "Start in {0} s...", "Empieza en {0} s...");
            A("clk_done", "Bitti: toplam {0} tıklama", "Done: {0} clicks in total", "Fertig: insgesamt {0} Klicks", "Terminado: {0} clics en total");
            A("clk_bad_interval", "Tıklama aralığı en az 1 ms olmalı.", "The click interval must be at least 1 ms.",
                "Das Klickintervall muss mindestens 1 ms betragen.", "El intervalo debe ser de al menos 1 ms.");
            A("clk_hotkey_fail", "{0} kısayolu başka bir program tarafından kullanılıyor.", "The {0} hotkey is already used by another program.",
                "Das Tastenkürzel {0} wird bereits von einem anderen Programm verwendet.", "El atajo {0} ya lo usa otro programa.");

            // Click sequence
            A("clk_pos_sequence", "Konum listesi (sırayla tıkla)", "A list of positions (click in order)",
                "Positionsliste (der Reihe nach klicken)", "Lista de posiciones (clic en orden)");
            A("seq_card", "Tıklama sırası", "Click sequence", "Klickfolge", "Secuencia de clics");
            A("seq_col_no", "#", "#", "#", "#");
            A("seq_col_x", "X", "X", "X", "X");
            A("seq_col_y", "Y", "Y", "Y", "Y");
            A("seq_col_button", "Tuş", "Button", "Taste", "Botón");
            A("seq_col_type", "Tür", "Type", "Art", "Tipo");
            A("seq_col_delay", "Sonra bekle", "Wait after", "Danach warten", "Esperar después");
            A("seq_delay", "Tıklamadan sonra bekle:", "Wait after the click:", "Nach dem Klick warten:", "Esperar tras el clic:");
            A("seq_add", "Listeye ekle", "Add to list", "Hinzufügen", "Añadir");
            A("seq_update", "Seçiliyi güncelle", "Update selected", "Auswahl ändern", "Actualizar selección");
            A("seq_remove", "Sil", "Remove", "Entfernen", "Quitar");
            A("seq_clear", "Temizle", "Clear", "Leeren", "Vaciar");
            A("seq_up", "▲ Yukarı", "▲ Up", "▲ Hoch", "▲ Subir");
            A("seq_down", "▼ Aşağı", "▼ Down", "▼ Runter", "▼ Bajar");
            A("seq_save", "Kaydet (.json)...", "Save (.json)...", "Speichern (.json)...", "Guardar (.json)...");
            A("seq_load", "Yükle (.json)...", "Load (.json)...", "Laden (.json)...", "Cargar (.json)...");
            A("seq_save_title", "Tıklama sırasını kaydet", "Save click sequence", "Klickfolge speichern", "Guardar secuencia de clics");
            A("seq_load_title", "Tıklama sırası yükle", "Load click sequence", "Klickfolge laden", "Cargar secuencia de clics");
            A("seq_hint", "F8 imlecin olduğu yeri yukarıdaki tuş, tür ve bekleme ile listeye yeni adım olarak ekler. Bekleme, o adımdaki tıklamadan sonra uygulanır.",
                "F8 adds the cursor position as a new step, using the button, type and wait above. The wait happens after that step's click.",
                "F8 fügt die Mausposition als neuen Schritt mit Taste, Art und Wartezeit von oben hinzu. Gewartet wird nach dem Klick dieses Schritts.",
                "F8 añade la posición del cursor como un paso nuevo, con el botón, tipo y espera de arriba. La espera ocurre tras el clic de ese paso.");
            A("seq_info", "{0} adım · bir tur ≈ {1}", "{0} steps · one round ≈ {1}", "{0} Schritte · eine Runde ≈ {1}", "{0} pasos · una vuelta ≈ {1}");
            A("seq_info_empty", "Liste boş. İmleci bir yere götürüp F8'e bas.", "The list is empty. Point at a spot and press F8.",
                "Die Liste ist leer. Zeig auf eine Stelle und drück F8.", "La lista está vacía. Señala un punto y pulsa F8.");
            A("seq_rep_rounds", "Listeyi belirli sayıda tekrarla:", "Repeat the list a set number of times:",
                "Liste feste Anzahl wiederholen:", "Repetir la lista un número de veces:");
            A("seq_running", "Çalışıyor: tur {0} · adım {1}/{2} · {3} tıklama", "Running: round {0} · step {1}/{2} · {3} clicks",
                "Läuft: Runde {0} · Schritt {1}/{2} · {3} Klicks", "En marcha: vuelta {0} · paso {1}/{2} · {3} clics");
            A("seq_empty", "Tıklama listesi boş. Önce en az bir konum ekle (F8).", "The click list is empty. Add at least one position first (F8).",
                "Die Klickliste ist leer. Füge zuerst mindestens eine Position hinzu (F8).", "La lista está vacía. Añade antes al menos una posición (F8).");
            A("seq_zero_delay", "Listedeki bekleme sürelerinin toplamı en az 1 ms olmalı.", "The waits in the list must add up to at least 1 ms.",
                "Die Wartezeiten der Liste müssen zusammen mindestens 1 ms ergeben.", "Las esperas de la lista deben sumar al menos 1 ms.");
            A("seq_clear_confirm", "Listedeki tüm adımlar silinsin mi?", "Remove all steps from the list?",
                "Alle Schritte aus der Liste entfernen?", "¿Quitar todos los pasos de la lista?");
            A("seq_load_fail", "Dosya yüklenemedi:\n{0}", "Could not load the file:\n{0}", "Datei konnte nicht geladen werden:\n{0}", "No se pudo cargar el archivo:\n{0}");
            A("seq_err_json", "Geçerli bir JSON dosyası değil.", "This is not a valid JSON file.", "Keine gültige JSON-Datei.", "No es un archivo JSON válido.");
            A("seq_err_nosteps", "Dosyada \"steps\" listesi bulunamadı.", "The file has no \"steps\" list.", "Die Datei enthält keine \"steps\"-Liste.", "El archivo no tiene una lista \"steps\".");
            A("seq_err_step", "{0}. adım geçersiz (x, y, button, clicks, delay_ms).", "Step {0} is invalid (x, y, button, clicks, delay_ms).",
                "Schritt {0} ist ungültig (x, y, button, clicks, delay_ms).", "El paso {0} no es válido (x, y, button, clicks, delay_ms).");

            // Bulk install
            A("nav_setup_title", "Toplu Kurulum Yöneticisi", "Bulk Install Manager", "Massen-Installationsmanager", "Gestor de instalación masiva");
            A("su_sub", "Seçtiğin programları winget ve kendi kurulum dosyalarınla sırayla kurar. Format sonrası tek tıkla hazır ol.",
                "Installs the programs you pick one after another, with winget or your own setup files. Be ready in one click after a fresh install.",
                "Installiert die gewählten Programme nacheinander, per winget oder mit eigenen Setup-Dateien. Nach einer Neuinstallation mit einem Klick bereit.",
                "Instala uno tras otro los programas que elijas, con winget o con tus propios instaladores. Listo en un clic tras formatear.");
            A("su_start", "Seçilenleri kur", "Install selected", "Auswahl installieren", "Instalar selección");
            A("su_stop", "Durdur", "Stop", "Stopp", "Detener");
            A("su_checking_winget", "winget denetleniyor...", "Checking winget...", "winget wird geprüft...", "Comprobando winget...");
            A("su_idle", "{0} program seçili.", "{0} programs selected.", "{0} Programme ausgewählt.", "{0} programas seleccionados.");
            A("su_running", "Kuruluyor {0}/{1}: {2}", "Installing {0}/{1}: {2}", "Installiere {0}/{1}: {2}", "Instalando {0}/{1}: {2}");
            A("su_stopping", "{0} bitince duracak...", "Stopping after {0}...", "Stoppt nach {0}...", "Se detendrá tras {0}...");
            A("su_summary", "Bitti: {0} kuruldu · {1} zaten vardı · {2} kurulamadı", "Done: {0} installed · {1} already there · {2} not installed",
                "Fertig: {0} installiert · {1} schon vorhanden · {2} nicht installiert", "Listo: {0} instalados · {1} ya estaban · {2} sin instalar");
            A("su_nothing", "Listede seçili program yok.", "No programs are selected.", "Keine Programme ausgewählt.", "No hay programas seleccionados.");
            A("su_no_winget", "winget (Windows Paket Yöneticisi) bulunamadı. Microsoft Store'daki \"Uygulama Yükleyicisi\" ile gelir.\n\nMağaza sayfası açılsın mı?",
                "winget (Windows Package Manager) was not found. It comes with \"App Installer\" from the Microsoft Store.\n\nOpen the Store page?",
                "winget (Windows-Paket-Manager) wurde nicht gefunden. Es kommt mit dem \"App-Installer\" aus dem Microsoft Store.\n\nStore-Seite öffnen?",
                "No se encontró winget (Administrador de paquetes de Windows). Viene con \"Instalador de aplicación\" de Microsoft Store.\n\n¿Abrir la página de la Store?");
            A("su_no_winget_short", "winget bulunamadı.", "winget was not found.", "winget wurde nicht gefunden.", "No se encontró winget.");
            A("su_card_list", "Kurulacak programlar", "Programs to install", "Zu installierende Programme", "Programas a instalar");
            A("su_col_name", "Program", "Program", "Programm", "Programa");
            A("su_col_source", "Kaynak", "Source", "Quelle", "Origen");
            A("su_col_status", "Durum", "Status", "Status", "Estado");
            A("su_col_id", "Winget kimliği", "Winget ID", "Winget-ID", "ID de winget");
            A("su_col_version", "Sürüm", "Version", "Version", "Versión");
            A("su_all", "Tümünü seç", "Select all", "Alle", "Todos");
            A("su_none_btn", "Hiçbiri", "None", "Keine", "Ninguno");
            A("su_save", "Listeyi kaydet (.json)...", "Save list (.json)...", "Liste speichern (.json)...", "Guardar lista (.json)...");
            A("su_load", "Liste yükle (.json)...", "Load list (.json)...", "Liste laden (.json)...", "Cargar lista (.json)...");
            A("su_save_title", "Kurulum listesini kaydet", "Save install list", "Installationsliste speichern", "Guardar lista de instalación");
            A("su_load_title", "Kurulum listesi yükle", "Load install list", "Installationsliste laden", "Cargar lista de instalación");
            A("su_default_file", "kurulum-listesi.json", "install-list.json", "installationsliste.json", "lista-instalacion.json");
            A("su_list_hint", "İpucu: listeyi format atmadan önce D: sürücüsüne ya da USB belleğe kaydet; yeni kurulumda yükleyip tek tıkla kur.",
                "Tip: save the list to another drive or a USB stick before you reinstall Windows, then load it and install everything in one click.",
                "Tipp: Speichere die Liste vor der Neuinstallation auf einem anderen Laufwerk oder USB-Stick, lade sie danach und installiere alles mit einem Klick.",
                "Consejo: guarda la lista en otra unidad o en un USB antes de reinstalar Windows; luego cárgala e instala todo en un clic.");
            A("su_card_add", "Listeye program ekle", "Add programs to the list", "Programme hinzufügen", "Añadir programas");
            A("su_search_label", "winget'te ara:", "Search winget:", "In winget suchen:", "Buscar en winget:");
            A("su_search", "Ara", "Search", "Suchen", "Buscar");
            A("su_searching", "Aranıyor...", "Searching...", "Suche...", "Buscando...");
            A("su_no_results", "Sonuç bulunamadı.", "No results.", "Keine Ergebnisse.", "Sin resultados.");
            A("su_add_winget", "Seçileni listeye ekle", "Add selected to the list", "Auswahl hinzufügen", "Añadir selección");
            A("su_search_hint", "Bir sonucu çift tıklayarak da ekleyebilirsin. Kimliği biliyorsan (ör. Google.Chrome) arama kutusuna yazıp doğrudan ekle.",
                "You can also double-click a result. If you know the ID (e.g. Google.Chrome), type it in the box and add it directly.",
                "Du kannst ein Ergebnis auch doppelklicken. Kennst du die ID (z. B. Google.Chrome), tippe sie ein und füge sie direkt hinzu.",
                "También puedes hacer doble clic en un resultado. Si conoces el ID (p. ej. Google.Chrome), escríbelo y añádelo directamente.");
            A("su_add_file", "Kurulum dosyası ekle...", "Add installer files...",
                "Installationsdateien hinzufügen...", "Añadir instaladores...");
            A("su_add_file_title", "Kurulum dosyalarını seç", "Choose setup files", "Setup-Dateien auswählen", "Elegir instaladores");
            A("su_args", "Parametreler:", "Arguments:", "Parameter:", "Parámetros:");
            A("su_args_apply", "Uygula", "Apply", "Übernehmen", "Aplicar");
            A("su_file_hint", "EXE, MSI, MSIX, MSIXBundle, APPX ve APPXBundle dosyaları eklenebilir. MSI ile bilinen Inno/NSIS EXE'leri otomatik tanınır; diğer EXE'ler kurulum penceresini açar. Sessiz kurulum anahtarını biliyorsan dosyayı seçip Parametreler alanına yaz. Sonuç dosyaya ve Windows uyumluluğuna bağlıdır.",
                "You can add EXE, MSI, MSIX, MSIXBundle, APPX and APPXBundle files. MSI and known Inno/NSIS EXE installers are detected; other EXE files open their setup window. If you know a silent switch, select the file and enter it under Arguments. Success depends on the installer and Windows compatibility.",
                "Du kannst EXE-, MSI-, MSIX-, MSIXBundle-, APPX- und APPXBundle-Dateien hinzufügen. MSI und bekannte Inno-/NSIS-EXE-Installer werden erkannt; andere EXE-Dateien öffnen ihr Setup. Bekannte Silent-Schalter kannst du unter Parameter eintragen. Der Erfolg hängt vom Installer und der Windows-Kompatibilität ab.",
                "Puedes añadir archivos EXE, MSI, MSIX, MSIXBundle, APPX y APPXBundle. Se detectan MSI y los instaladores EXE conocidos de Inno/NSIS; otros EXE abren su propia ventana. Si conoces el parámetro silencioso, selecciónalo e introdúcelo en Parámetros. El resultado depende del instalador y de la compatibilidad con Windows.");
            A("su_user_only", "Yalnızca benim için kur (yönetici izni sorma)", "Install for me only (never ask for admin rights)",
                "Nur für mich installieren (nie nach Adminrechten fragen)", "Instalar solo para mí (sin pedir permisos de administrador)");
            A("su_user_hint", "Açıkken programlar kullanıcı kapsamına kurulur; yönetici isteyenler atlanır ve UAC açılmaz. Kapalıyken kurulum dosyaları yönetici izni isteyebilir. MSIX/APPX paketleri Windows gereği geçerli kullanıcıya kurulur.",
                "When enabled, installers use per-user scope; packages that require admin rights are skipped and no UAC prompt appears. When disabled, installer files may request admin rights. Windows installs MSIX/APPX packages for the current user.",
                "Aktiviert werden Installer im Benutzerbereich ausgeführt; Pakete mit Adminbedarf werden übersprungen und UAC erscheint nicht. Deaktiviert können Installer Adminrechte anfordern. Windows installiert MSIX/APPX für den aktuellen Benutzer.",
                "Activado, los instaladores usan el ámbito del usuario; se omiten los que requieren administrador y no aparece UAC. Desactivado, los archivos pueden solicitar permisos. Windows instala MSIX/APPX para el usuario actual.");
            A("su_uac_cancelled", "Yönetici onayı verilmedi; kurulum atlandı.", "Administrator approval was cancelled; installation skipped.", "Administratorbestätigung abgebrochen; Installation übersprungen.", "Se canceló la aprobación; instalación omitida.");
            A("su_card_log", "Kayıt", "Log", "Protokoll", "Registro");
            A("su_log_start", "{0} program kurulacak", "installing {0} programs", "{0} Programme werden installiert", "se instalarán {0} programas");
            A("su_exit_confirm", "Toplu kurulum sürüyor; çıkarsan sıradaki programlar kurulmayacak. Yine de çıkılsın mı?",
                "A bulk install is running; if you exit, the remaining programs will not be installed. Exit anyway?",
                "Eine Massen-Installation läuft; beim Beenden werden die restlichen Programme nicht installiert. Trotzdem beenden?",
                "Hay una instalación en curso; si sales, no se instalarán los programas restantes. ¿Salir de todos modos?");
            A("su_err_noitems", "Dosyada \"items\" listesi bulunamadı.", "The file has no \"items\" list.", "Die Datei enthält keine \"items\"-Liste.", "El archivo no tiene una lista \"items\".");
            A("su_err_item", "{0}. öğe geçersiz (\"winget\" ya da \"file\" gerekli).", "Item {0} is invalid (needs \"winget\" or \"file\").",
                "Eintrag {0} ist ungültig (\"winget\" oder \"file\" nötig).", "El elemento {0} no es válido (requiere \"winget\" o \"file\").");
            A("su_st_waiting", "Sırada", "Queued", "Wartet", "En cola");
            A("su_st_running", "Kuruluyor...", "Installing...", "Wird installiert...", "Instalando...");
            A("su_st_installed", "✓ Kuruldu", "✓ Installed", "✓ Installiert", "✓ Instalado");
            A("su_st_installedrestart", "✓ Kuruldu (yeniden başlat)", "✓ Installed (restart)", "✓ Installiert (Neustart)", "✓ Instalado (reiniciar)");
            A("su_st_alreadyinstalled", "✓ Zaten kurulu", "✓ Already installed", "✓ Bereits installiert", "✓ Ya instalado");
            A("su_st_needsadmin", "Yönetici izni gerekiyor, atlandı", "Needs admin rights, skipped", "Adminrechte nötig, übersprungen", "Requiere administrador, omitido");
            A("su_st_notfound", "winget'te bulunamadı", "Not found in winget", "In winget nicht gefunden", "No está en winget");
            A("su_st_filemissing", "Dosya bulunamadı", "File not found", "Datei nicht gefunden", "Archivo no encontrado");
            A("su_st_inuse", "Program açık, kapatıp tekrar dene", "Program is running, close it and retry", "Programm läuft, schließen und erneut versuchen", "El programa está abierto; ciérralo y reintenta");
            A("su_st_nonetwork", "İnternet bağlantısı yok", "No internet connection", "Keine Internetverbindung", "Sin conexión a internet");
            A("su_st_failed", "Hata", "Failed", "Fehler", "Error");
            A("su_st_timedout", "Zaman aşımı", "Timed out", "Zeitüberschreitung", "Tiempo agotado");
            A("su_st_skipped", "Durduruldu", "Stopped", "Gestoppt", "Detenido");

            // Shutdown timer
            A("sd_sub", "Süre dolunca bilgisayarı kapatır, yeniden başlatır veya uyutur.",
                "Shuts down, restarts or sleeps the PC when the time is up.",
                "Fährt den PC nach Ablauf der Zeit herunter, startet neu oder lässt ihn schlafen.",
                "Apaga, reinicia o suspende el PC cuando se acaba el tiempo.");
            A("sd_card_action", "Ne yapılsın?", "What should happen?", "Was soll passieren?", "¿Qué debe pasar?");
            A("act_shutdown", "Bilgisayarı kapat", "Shut down", "Herunterfahren", "Apagar");
            A("act_restart", "Yeniden başlat", "Restart", "Neu starten", "Reiniciar");
            A("act_sleep", "Uyku moduna al", "Sleep", "Energie sparen", "Suspender");
            A("act_hibernate", "Hazırda beklet", "Hibernate", "Ruhezustand", "Hibernar");
            A("act_lock", "Ekranı kilitle", "Lock the screen", "Bildschirm sperren", "Bloquear la pantalla");
            A("act_logoff", "Oturumu kapat", "Sign out", "Abmelden", "Cerrar sesión");
            A("sd_card_when", "Ne zaman?", "When?", "Wann?", "¿Cuándo?");
            A("sd_after", "Şu kadar süre sonra:", "After:", "Nach:", "Dentro de:");
            A("sd_at", "Şu saatte:", "At:", "Um:", "A las:");
            A("sd_quick", "Hızlı seçim:", "Quick pick:", "Schnellwahl:", "Selección rápida:");
            A("sd_force", "Açık uygulamaları zorla kapat (kaydedilmemiş işler kaybolabilir)",
                "Force-close open apps (unsaved work may be lost)",
                "Offene Apps zwangsweise schließen (Ungespeichertes kann verloren gehen)",
                "Forzar el cierre de las apps (se puede perder lo no guardado)");
            A("sd_warn", "Son 1 dakikada iptal penceresi göster", "Show a cancel window 1 minute before",
                "1 Minute vorher ein Abbruchfenster zeigen", "Mostrar una ventana para cancelar 1 minuto antes");
            A("sd_start", "Zamanlayıcıyı başlat", "Start timer", "Timer starten", "Iniciar temporizador");
            A("sd_cancel", "İptal et", "Cancel", "Abbrechen", "Cancelar");
            A("sd_idle", "Zamanlayıcı kurulu değil", "No timer set", "Kein Timer aktiv", "Sin temporizador");
            A("sd_scheduled", "{0} — saat {1}", "{0} at {1}", "{0} um {1}", "{0} a las {1}");
            A("sd_zero", "Süre sıfırdan büyük olmalı.", "The time must be greater than zero.",
                "Die Zeit muss größer als null sein.", "El tiempo debe ser mayor que cero.");
            A("sd_warn_msg", "{0} 1 dakika içinde gerçekleşecek.", "{0} will happen in less than a minute.",
                "{0} erfolgt in weniger als einer Minute.", "{0} se realizará en menos de un minuto.");
            A("sd_now", "Şimdi yap", "Do it now", "Jetzt ausführen", "Hacerlo ahora");
            A("sd_exit_confirm", "Kapatma zamanlayıcısı çalışıyor ve çıkarsan iptal olacak. Yine de çıkılsın mı?",
                "A shutdown timer is running and will be cancelled if you exit. Exit anyway?",
                "Ein Herunterfahr-Timer läuft und wird beim Beenden abgebrochen. Trotzdem beenden?",
                "Hay un temporizador activo y se cancelará si sales. ¿Salir de todos modos?");

            // Keep awake
            A("aw_sub", "Bilgisayarın uykuya geçmesini ve ekranın kapanmasını engeller.",
                "Stops the PC from going to sleep and the screen from turning off.",
                "Verhindert, dass der PC schläft und der Bildschirm ausgeht.",
                "Evita que el PC se suspenda y que la pantalla se apague.");
            A("aw_on", "AÇIK — bilgisayar uyumayacak", "ON — the PC will stay awake", "AN — der PC bleibt wach", "SÍ — el PC no se suspenderá");
            A("aw_off", "KAPALI", "OFF", "AUS", "NO");
            A("aw_until", "{0} saatine kadar", "until {0}", "bis {0}", "hasta las {0}");
            A("aw_enable", "Etkinleştir", "Turn on", "Einschalten", "Activar");
            A("aw_disable", "Kapat", "Turn off", "Ausschalten", "Desactivar");
            A("aw_card_options", "Seçenekler", "Options", "Optionen", "Opciones");
            A("aw_display", "Ekranı da açık tut", "Keep the screen on too", "Bildschirm ebenfalls anlassen", "Mantener también la pantalla encendida");
            A("aw_jiggle", "Fareyi dakikada bir 1 piksel oynat (boşta kalma süresini ölçen uygulamalar için)",
                "Nudge the mouse 1 px every minute (for apps that measure idle time)",
                "Maus jede Minute um 1 px bewegen (für Apps, die Leerlauf messen)",
                "Mover el ratón 1 px cada minuto (para apps que miden la inactividad)");
            A("aw_forever", "Ben kapatana kadar", "Until I turn it off", "Bis ich es ausschalte", "Hasta que lo desactive");
            A("aw_for", "Belirli süre (dk):", "For a while (min):", "Eine Weile (Min):", "Durante (min):");

            // Reminders
            A("rm_sub", "Belirlediğin süre sonunda ekranda bir bildirim gösterir.",
                "Shows a notification on screen after the time you set.",
                "Zeigt nach der eingestellten Zeit eine Benachrichtigung an.",
                "Muestra un aviso en pantalla tras el tiempo que indiques.");
            A("rm_card_new", "Yeni hatırlatıcı", "New reminder", "Neue Erinnerung", "Nuevo recordatorio");
            A("rm_message", "Mesaj:", "Message:", "Nachricht:", "Mensaje:");
            A("rm_default", "Mola zamanı! Biraz kalk ve esne.", "Time for a break! Stand up and stretch.",
                "Zeit für eine Pause! Steh auf und streck dich.", "¡Hora de un descanso! Levántate y estírate.");
            A("rm_after", "Kaç dakika sonra:", "In (minutes):", "In (Minuten):", "Dentro de (min):");
            A("rm_repeat", "Tekrarla", "Repeat", "Wiederholen", "Repetir");
            A("rm_sound", "Sesli uyar", "Play a sound", "Ton abspielen", "Reproducir sonido");
            A("rm_add", "Ekle", "Add", "Hinzufügen", "Añadir");
            A("rm_card_list", "Aktif hatırlatıcılar", "Active reminders", "Aktive Erinnerungen", "Recordatorios activos");
            A("rm_remove", "Seçileni sil", "Remove selected", "Auswahl entfernen", "Eliminar seleccionado");
            A("rm_col_msg", "Mesaj", "Message", "Nachricht", "Mensaje");
            A("rm_col_next", "Sıradaki", "Next", "Nächste", "Próximo");
            A("rm_col_repeat", "Tekrar", "Repeat", "Wiederholung", "Repetición");
            A("rm_every", "her {0} dk", "every {0} min", "alle {0} Min", "cada {0} min");
            A("rm_once", "bir kez", "once", "einmal", "una vez");
            A("rm_title", "Hatırlatıcı", "Reminder", "Erinnerung", "Recordatorio");
            A("rm_close", "Tamam", "OK", "OK", "Vale");

            // Settings
            A("st_sub", "Dil, görünüm, başlangıç ve kurulum.", "Language, appearance, startup and installation.",
                "Sprache, Aussehen, Autostart und Installation.", "Idioma, apariencia, inicio e instalación.");
            A("st_card_general", "Genel", "General", "Allgemein", "General");
            A("st_language", "Dil:", "Language:", "Sprache:", "Idioma:");
            A("st_theme", "Tema:", "Theme:", "Design:", "Tema:");
            A("th_dark", "Koyu", "Dark", "Dunkel", "Oscuro");
            A("th_light", "Açık", "Light", "Hell", "Claro");
            A("st_ontop", "Pencere her zaman üstte", "Keep the window on top", "Fenster immer im Vordergrund", "Ventana siempre visible");
            A("st_tray", "Kapatınca sistem tepsisinde çalışmaya devam et", "Keep running in the system tray when closed",
                "Beim Schließen im Infobereich weiterlaufen", "Seguir en la bandeja del sistema al cerrar");
            A("st_autostart", "Windows açılınca başlat (tepside)", "Start with Windows (in the tray)",
                "Mit Windows starten (im Infobereich)", "Iniciar con Windows (en la bandeja)");
            A("st_hotkeys", "Kısayollar: F6 = başlat/durdur · F8 = konumu kaydet. Tuş doluysa Ctrl+Alt ile otomatik denenir.",
                "Hotkeys: F6 = start/stop · F8 = save position. If a key is busy, Ctrl+Alt is tried automatically.",
                "Tasten: F6 = Start/Stopp · F8 = Position speichern. Bei Belegung wird automatisch Ctrl+Alt versucht.",
                "Atajos: F6 = iniciar/detener · F8 = guardar posición. Si están ocupadas, se prueba Ctrl+Alt automáticamente.");
            A("st_card_install", "Kurulum (yönetici izni gerekmez)", "Installation (no admin rights needed)",
                "Installation (keine Administratorrechte nötig)", "Instalación (sin permisos de administrador)");
            A("uac_setup_card", "Yönetici kısayolu", "Administrator shortcut", "Administratorkürzel", "Acceso de administrador");
            A("uac_setup_button", "UAC Sormadan Çalıştırma Kısayolu Oluştur (Tek Seferlik Kurulum)", "Create Shortcut to Run Without UAC Prompt (One-Time Setup)", "Verknüpfung ohne UAC-Abfrage erstellen (einmalige Einrichtung)", "Crear acceso directo sin aviso UAC (configuración única)");
            A("uac_setup_desc", "Bir kez yönetici onayı vererek bu uygulama için en yüksek ayrıcalıklı, yalnızca oturum açmış kullanıcıda çalışacak bir görev oluşturur ve masaüstüne görev kısayolu ekler. Uygulamayı başka konuma taşırsan kurulumu tekrarla.",
                "After one administrator approval, creates a highest-privilege task for this app, available only while you are signed in, and adds a desktop shortcut. Repeat setup if you move the app.",
                "Nach einmaliger Administratorbestätigung wird eine Aufgabe mit höchsten Rechten erstellt, die nur während deiner Anmeldung verfügbar ist, und eine Desktopverknüpfung angelegt. Nach dem Verschieben erneut einrichten.",
                "Tras una confirmación de administrador, crea una tarea con privilegios máximos disponible solo durante tu sesión e incluye un acceso directo en el escritorio. Repite la configuración si mueves la aplicación.");
            A("uac_setup_ok", "Görev Zamanlayıcı kaydı ve masaüstü kısayolu oluşturuldu. Bundan sonra uygulamayı masaüstündeki OtomasyonAraci kısayoluyla açabilirsin.",
                "The Task Scheduler entry and desktop shortcut were created. Use the OtomasyonAraci shortcut on your desktop to open the app from now on.",
                "Der Taskplanereintrag und die Desktopverknüpfung wurden erstellt. Öffne die App künftig über die Desktopverknüpfung OtomasyonAraci.",
                "Se crearon la tarea y el acceso directo. A partir de ahora, abre la aplicación con el acceso directo OtomasyonAraci del escritorio.");
            A("uac_setup_fail", "Kısayol oluşturulamadı: {0}", "Could not create the shortcut: {0}", "Die Verknüpfung konnte nicht erstellt werden: {0}", "No se pudo crear el acceso directo: {0}");
            A("uac_setup_not_admin", "Kurulum yönetici yetkisiyle başlatılmadı.", "Setup was not started with administrator rights.", "Die Einrichtung wurde nicht mit Administratorrechten gestartet.", "La configuración no se inició con permisos de administrador.");
            A("st_install_desc", "Companian'ı yalnızca senin kullanıcı hesabına kurar: %LOCALAPPDATA%\\Programs\\Companian klasörüne kopyalar, Başlat menüsüne kısayol ekler ve Yüklü uygulamalar listesine kaydeder. Her şey kendi profilinde olduğu için UAC izni sorulmaz. İstersen kurmadan, exe'yi doğrudan çalıştırarak taşınabilir şekilde de kullanabilirsin.",
                "Installs Companian for your user account only: copies it to %LOCALAPPDATA%\\Programs\\Companian, adds a Start menu shortcut and registers it under Installed apps. Everything stays in your own profile, so Windows never shows a UAC prompt. You can also skip installing and just run the exe as a portable app.",
                "Installiert Companian nur für dein Benutzerkonto: kopiert es nach %LOCALAPPDATA%\\Programs\\Companian, legt eine Startmenü-Verknüpfung an und trägt es unter Installierte Apps ein. Alles bleibt in deinem Profil, daher erscheint keine UAC-Abfrage. Du kannst die exe auch ohne Installation portabel nutzen.",
                "Instala Companian solo para tu cuenta: lo copia en %LOCALAPPDATA%\\Programs\\Companian, añade un acceso en el menú Inicio y lo registra en Aplicaciones instaladas. Todo queda en tu perfil, así que Windows no muestra el aviso de UAC. También puedes usar el exe directamente, en modo portátil.");
            A("st_desktop", "Masaüstüne kısayol ekle", "Add a desktop shortcut", "Desktop-Verknüpfung anlegen", "Crear acceso directo en el escritorio");
            A("st_install", "Bu kullanıcı için kur", "Install for this user", "Für diesen Benutzer installieren", "Instalar para este usuario");
            A("st_reinstall", "Kurulumu güncelle", "Update installation", "Installation aktualisieren", "Actualizar instalación");
            A("st_uninstall", "Kaldır", "Uninstall", "Deinstallieren", "Desinstalar");
            A("st_installed", "Kurulu: {0}", "Installed in: {0}", "Installiert in: {0}", "Instalado en: {0}");
            A("st_portable", "Kurulu değil, taşınabilir modda çalışıyor.", "Not installed, running as a portable app.",
                "Nicht installiert, läuft portabel.", "No instalado, se ejecuta en modo portátil.");
            A("st_install_ok", "Kurulum tamamlandı. Kurulan kopya şimdi açılsın mı?", "Installed. Open the installed copy now?",
                "Installiert. Installierte Version jetzt öffnen?", "Instalado. ¿Abrir ahora la copia instalada?");
            A("st_install_done", "Kurulum güncellendi.", "Installation updated.", "Installation aktualisiert.", "Instalación actualizada.");
            A("st_uninstall_confirm", "Companian bu kullanıcıdan kaldırılsın mı? Ayarlar da silinecek.",
                "Remove Companian for this user? Your settings will be deleted too.",
                "Companian für diesen Benutzer entfernen? Die Einstellungen werden ebenfalls gelöscht.",
                "¿Quitar Companian para este usuario? También se borrarán los ajustes.");
            A("st_uninstall_ok", "Companian kaldırıldı.", "Companian was uninstalled.", "Companian wurde deinstalliert.", "Companian se ha desinstalado.");
            A("st_card_about", "Hakkında", "About", "Über", "Acerca de");
            A("st_about", "Companian v{0} · Açık kaynak, MIT lisansı", "Companian v{0} · Open source, MIT license",
                "Companian v{0} · Open Source, MIT-Lizenz", "Companian v{0} · Código abierto, licencia MIT");
            A("st_github", "GitHub sayfasını aç", "Open the GitHub page", "GitHub-Seite öffnen", "Abrir la página de GitHub");

            // Tray / misc
            A("tr_show", "Göster", "Show", "Anzeigen", "Mostrar");
            A("tr_exit", "Çıkış", "Exit", "Beenden", "Salir");
            A("tr_running", "Companian tepside çalışmaya devam ediyor.", "Companian keeps running in the tray.",
                "Companian läuft im Infobereich weiter.", "Companian sigue en la bandeja del sistema.");
            A("err", "Hata", "Error", "Fehler", "Error");
        }
    }
}
