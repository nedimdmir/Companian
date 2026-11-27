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
            A("st_hotkeys", "Kısayollar: F6 = tıklayıcıyı başlat/durdur · F8 = imleç konumunu kaydet",
                "Hotkeys: F6 = start/stop the clicker · F8 = save the cursor position",
                "Tastenkürzel: F6 = Klicker starten/stoppen · F8 = Mausposition speichern",
                "Atajos: F6 = iniciar/detener · F8 = guardar la posición del cursor");
            A("st_card_install", "Kurulum (yönetici izni gerekmez)", "Installation (no admin rights needed)",
                "Installation (keine Administratorrechte nötig)", "Instalación (sin permisos de administrador)");
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
