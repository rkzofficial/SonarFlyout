# Writes Strings/<lang>/Resources.resw for each language below from the keys in Strings/en-US/Resources.resw.
# Product names (Sonar, SteelSeries GG) and Sonar's mode names (Classic, Streamer) are deliberately not translated.
import re
import sys
from pathlib import Path
from xml.sax.saxutils import escape

A = "[using:Microsoft.UI.Xaml.Automation]AutomationProperties.Name"
T = "[using:Microsoft.UI.Xaml.Controls]ToolTipService.ToolTip"

KEYS = [
    "AppDescription", "WindowTitle", "TrayTooltip", "TrayVolume", "TrayMuted",
    "MenuOpenGG", "MenuStartWithWindows", "MenuStartWithWindowsOn", "MenuStartWithWindowsOff", "MenuExit",
    "SoundHeader.Text", "OutputHeader.Text", "PerChannelHint.Text", f"SyncOutputs.{A}", f"Outputs.{A}",
    "MixerHeader.Text", "InputHeader.Text", f"Inputs.{A}", f"OpenGG.{T}", f"OpenGG.{A}",
    "SyncedTooltip", "UnsyncedTooltip",
    "ChannelMaster", "ChannelGame", "ChannelChat", "ChannelMedia", "ChannelAux", "ChannelMic",
    "ChannelVolumeName", "ChannelMuteName", "ChannelOutputName", "Mute", "Unmute", "UnavailableDevice",
    "ErrorUnreachable", "ErrorStreamerMode",
]

LANGUAGES = {
    "de-DE": [
        "Tray-Mixer für SteelSeries Sonar", "Sonar-Mixer", "Sonar-Mixer", "Sonar: {0}", "Sonar: stumm",
        "SteelSeries GG öffnen", "Mit Windows starten", "Mit Windows starten, ein", "Mit Windows starten, aus", "Beenden",
        "Sound", "Ausgabe", "Pro Kanal", "Gleiche Ausgabe für alle Kanäle", "Ausgabegerät",
        "Mixer", "Eingabe", "Eingabegerät", "SteelSeries GG öffnen", "SteelSeries GG öffnen",
        "Ausgaben synchronisiert. Klicken, um pro Kanal ein Gerät zu wählen.", "Alle Kanäle mit einer Ausgabe synchronisieren",
        "Master", "Spiel", "Chat", "Medien", "Aux", "Mikrofon",
        "{0}-Lautstärke", "{0} stummschalten", "{0}-Ausgabe", "Stummschalten", "Stummschaltung aufheben", "Nicht verfügbares Gerät",
        "Sonar ist nicht erreichbar. Läuft SteelSeries GG?", "Der Streamer-Modus wird noch nicht unterstützt. Stelle Sonar auf den Classic-Modus um.",
    ],
    "fr-FR": [
        "Mélangeur de la zone de notification pour SteelSeries Sonar", "Mélangeur Sonar", "Mélangeur Sonar", "Sonar : {0}", "Sonar : muet",
        "Ouvrir SteelSeries GG", "Lancer au démarrage de Windows", "Lancer au démarrage de Windows, activé", "Lancer au démarrage de Windows, désactivé", "Quitter",
        "Son", "Sortie", "Par canal", "Même sortie pour tous les canaux", "Périphérique de sortie",
        "Mélangeur", "Entrée", "Périphérique d’entrée", "Ouvrir SteelSeries GG", "Ouvrir SteelSeries GG",
        "Sorties synchronisées. Cliquez pour choisir un périphérique par canal.", "Synchroniser tous les canaux sur une seule sortie",
        "Principal", "Jeu", "Chat", "Média", "Aux", "Micro",
        "Volume {0}", "Couper {0}", "Sortie {0}", "Couper le son", "Rétablir le son", "Périphérique indisponible",
        "Impossible de joindre Sonar. SteelSeries GG est-il lancé ?", "Le mode Streamer n’est pas encore pris en charge. Passez Sonar en mode Classic.",
    ],
    "es-ES": [
        "Mezclador en la bandeja del sistema para SteelSeries Sonar", "Mezclador de Sonar", "Mezclador de Sonar", "Sonar: {0}", "Sonar: silenciado",
        "Abrir SteelSeries GG", "Iniciar con Windows", "Iniciar con Windows, activado", "Iniciar con Windows, desactivado", "Salir",
        "Sonido", "Salida", "Por canal", "Misma salida para todos los canales", "Dispositivo de salida",
        "Mezclador", "Entrada", "Dispositivo de entrada", "Abrir SteelSeries GG", "Abrir SteelSeries GG",
        "Salidas sincronizadas. Haz clic para elegir un dispositivo por canal.", "Sincronizar todos los canales en una salida",
        "Maestro", "Juego", "Chat", "Multimedia", "Aux", "Micrófono",
        "Volumen de {0}", "Silenciar {0}", "Salida de {0}", "Silenciar", "Activar sonido", "Dispositivo no disponible",
        "No se puede conectar con Sonar. ¿Está abierto SteelSeries GG?", "El modo Streamer aún no es compatible. Cambia Sonar al modo Classic.",
    ],
    "it-IT": [
        "Mixer nell'area di notifica per SteelSeries Sonar", "Mixer Sonar", "Mixer Sonar", "Sonar: {0}", "Sonar: disattivato",
        "Apri SteelSeries GG", "Avvia con Windows", "Avvia con Windows, attivo", "Avvia con Windows, disattivo", "Esci",
        "Audio", "Uscita", "Per canale", "Stessa uscita per tutti i canali", "Dispositivo di uscita",
        "Mixer", "Ingresso", "Dispositivo di ingresso", "Apri SteelSeries GG", "Apri SteelSeries GG",
        "Uscite sincronizzate. Fai clic per scegliere un dispositivo per canale.", "Sincronizza tutti i canali su un'unica uscita",
        "Master", "Gioco", "Chat", "Media", "Aux", "Microfono",
        "Volume {0}", "Disattiva audio {0}", "Uscita {0}", "Disattiva audio", "Riattiva audio", "Dispositivo non disponibile",
        "Impossibile raggiungere Sonar. SteelSeries GG è in esecuzione?", "La modalità Streamer non è ancora supportata. Passa Sonar alla modalità Classic.",
    ],
    "pt-BR": [
        "Mixer na bandeja do sistema para o SteelSeries Sonar", "Mixer do Sonar", "Mixer do Sonar", "Sonar: {0}", "Sonar: sem som",
        "Abrir o SteelSeries GG", "Iniciar com o Windows", "Iniciar com o Windows, ativado", "Iniciar com o Windows, desativado", "Sair",
        "Som", "Saída", "Por canal", "Mesma saída para todos os canais", "Dispositivo de saída",
        "Mixer", "Entrada", "Dispositivo de entrada", "Abrir o SteelSeries GG", "Abrir o SteelSeries GG",
        "Saídas sincronizadas. Clique para escolher um dispositivo por canal.", "Sincronizar todos os canais em uma saída",
        "Master", "Jogo", "Chat", "Mídia", "Aux", "Microfone",
        "Volume de {0}", "Silenciar {0}", "Saída de {0}", "Silenciar", "Ativar som", "Dispositivo indisponível",
        "Não foi possível acessar o Sonar. O SteelSeries GG está em execução?", "O modo Streamer ainda não é compatível. Mude o Sonar para o modo Classic.",
    ],
    "ru-RU": [
        "Микшер в области уведомлений для SteelSeries Sonar", "Микшер Sonar", "Микшер Sonar", "Sonar: {0}", "Sonar: звук выключен",
        "Открыть SteelSeries GG", "Запускать вместе с Windows", "Запускать вместе с Windows, вкл.", "Запускать вместе с Windows, выкл.", "Выход",
        "Звук", "Вывод", "По каналам", "Один вывод для всех каналов", "Устройство вывода",
        "Микшер", "Ввод", "Устройство ввода", "Открыть SteelSeries GG", "Открыть SteelSeries GG",
        "Выводы синхронизированы. Нажмите, чтобы выбрать устройство для каждого канала.", "Синхронизировать все каналы с одним выводом",
        "Общий", "Игра", "Чат", "Медиа", "Aux", "Микрофон",
        "Громкость: {0}", "Выключить звук: {0}", "Вывод: {0}", "Выключить звук", "Включить звук", "Устройство недоступно",
        "Не удаётся подключиться к Sonar. Запущен ли SteelSeries GG?", "Режим Streamer пока не поддерживается. Переключите Sonar в режим Classic.",
    ],
    "pl-PL": [
        "Mikser w zasobniku systemowym dla SteelSeries Sonar", "Mikser Sonar", "Mikser Sonar", "Sonar: {0}", "Sonar: wyciszony",
        "Otwórz SteelSeries GG", "Uruchamiaj z systemem Windows", "Uruchamiaj z systemem Windows, wł.", "Uruchamiaj z systemem Windows, wył.", "Zakończ",
        "Dźwięk", "Wyjście", "Dla każdego kanału", "To samo wyjście dla wszystkich kanałów", "Urządzenie wyjściowe",
        "Mikser", "Wejście", "Urządzenie wejściowe", "Otwórz SteelSeries GG", "Otwórz SteelSeries GG",
        "Wyjścia zsynchronizowane. Kliknij, aby wybrać urządzenie dla każdego kanału.", "Synchronizuj wszystkie kanały z jednym wyjściem",
        "Główny", "Gra", "Czat", "Multimedia", "Aux", "Mikrofon",
        "Głośność: {0}", "Wycisz: {0}", "Wyjście: {0}", "Wycisz", "Wyłącz wyciszenie", "Niedostępne urządzenie",
        "Nie można połączyć się z Sonar. Czy SteelSeries GG jest uruchomiony?", "Tryb Streamer nie jest jeszcze obsługiwany. Przełącz Sonar w tryb Classic.",
    ],
    "tr-TR": [
        "SteelSeries Sonar için sistem tepsisi karıştırıcısı", "Sonar karıştırıcı", "Sonar karıştırıcı", "Sonar: {0}", "Sonar: sessiz",
        "SteelSeries GG'yi aç", "Windows ile başlat", "Windows ile başlat, açık", "Windows ile başlat, kapalı", "Çık",
        "Ses", "Çıkış", "Kanal başına", "Tüm kanallar için aynı çıkış", "Çıkış aygıtı",
        "Karıştırıcı", "Giriş", "Giriş aygıtı", "SteelSeries GG'yi aç", "SteelSeries GG'yi aç",
        "Çıkışlar eşitlendi. Kanal başına aygıt seçmek için tıklayın.", "Tüm kanalları tek çıkışa eşitle",
        "Ana", "Oyun", "Sohbet", "Medya", "Aux", "Mikrofon",
        "{0} ses düzeyi", "{0} sesini kapat", "{0} çıkışı", "Sesi kapat", "Sesi aç", "Kullanılamayan aygıt",
        "Sonar'a ulaşılamıyor. SteelSeries GG çalışıyor mu?", "Streamer modu henüz desteklenmiyor. Sonar'ı Classic moduna geçirin.",
    ],
    "ja-JP": [
        "SteelSeries Sonar 用のタスク トレイ ミキサー", "Sonar ミキサー", "Sonar ミキサー", "Sonar: {0}", "Sonar: ミュート",
        "SteelSeries GG を開く", "Windows の起動時に開始", "Windows の起動時に開始、オン", "Windows の起動時に開始、オフ", "終了",
        "サウンド", "出力", "チャンネルごと", "すべてのチャンネルで同じ出力", "出力デバイス",
        "ミキサー", "入力", "入力デバイス", "SteelSeries GG を開く", "SteelSeries GG を開く",
        "出力は同期されています。クリックするとチャンネルごとにデバイスを選べます。", "すべてのチャンネルを 1 つの出力に同期",
        "マスター", "ゲーム", "チャット", "メディア", "Aux", "マイク",
        "{0} の音量", "{0} をミュート", "{0} の出力", "ミュート", "ミュート解除", "使用できないデバイス",
        "Sonar に接続できません。SteelSeries GG は起動していますか?", "Streamer モードにはまだ対応していません。Sonar を Classic モードに切り替えてください。",
    ],
    "ko-KR": [
        "SteelSeries Sonar용 트레이 믹서", "Sonar 믹서", "Sonar 믹서", "Sonar: {0}", "Sonar: 음소거",
        "SteelSeries GG 열기", "Windows 시작 시 실행", "Windows 시작 시 실행, 켜짐", "Windows 시작 시 실행, 꺼짐", "종료",
        "사운드", "출력", "채널별", "모든 채널에 같은 출력 사용", "출력 장치",
        "믹서", "입력", "입력 장치", "SteelSeries GG 열기", "SteelSeries GG 열기",
        "출력이 동기화되었습니다. 클릭하면 채널별로 장치를 선택할 수 있습니다.", "모든 채널을 하나의 출력으로 동기화",
        "마스터", "게임", "채팅", "미디어", "Aux", "마이크",
        "{0} 볼륨", "{0} 음소거", "{0} 출력", "음소거", "음소거 해제", "사용할 수 없는 장치",
        "Sonar에 연결할 수 없습니다. SteelSeries GG가 실행 중인가요?", "Streamer 모드는 아직 지원되지 않습니다. Sonar를 Classic 모드로 전환하세요.",
    ],
    "zh-Hans": [
        "SteelSeries Sonar 托盘混音器", "Sonar 混音器", "Sonar 混音器", "Sonar: {0}", "Sonar: 已静音",
        "打开 SteelSeries GG", "随 Windows 启动", "随 Windows 启动，开", "随 Windows 启动，关", "退出",
        "声音", "输出", "按通道", "所有通道使用同一输出", "输出设备",
        "混音器", "输入", "输入设备", "打开 SteelSeries GG", "打开 SteelSeries GG",
        "输出已同步。单击可为每个通道选择设备。", "将所有通道同步到一个输出",
        "主音量", "游戏", "聊天", "媒体", "Aux", "麦克风",
        "{0}音量", "{0}静音", "{0}输出", "静音", "取消静音", "设备不可用",
        "无法连接到 Sonar。SteelSeries GG 是否正在运行？", "尚不支持 Streamer 模式。请将 Sonar 切换到 Classic 模式。",
    ],
    "zh-Hant": [
        "SteelSeries Sonar 系統匣混音器", "Sonar 混音器", "Sonar 混音器", "Sonar: {0}", "Sonar: 已靜音",
        "開啟 SteelSeries GG", "隨 Windows 啟動", "隨 Windows 啟動，開", "隨 Windows 啟動，關", "結束",
        "音效", "輸出", "依通道", "所有通道使用相同輸出", "輸出裝置",
        "混音器", "輸入", "輸入裝置", "開啟 SteelSeries GG", "開啟 SteelSeries GG",
        "輸出已同步。按一下即可為每個通道選擇裝置。", "將所有通道同步到單一輸出",
        "主音量", "遊戲", "聊天", "媒體", "Aux", "麥克風",
        "{0}音量", "{0}靜音", "{0}輸出", "靜音", "取消靜音", "無法使用的裝置",
        "無法連線到 Sonar。SteelSeries GG 是否正在執行？", "尚不支援 Streamer 模式。請將 Sonar 切換為 Classic 模式。",
    ],
}

root = Path(__file__).resolve().parent.parent / "Strings"
english = (root / "en-US" / "Resources.resw").read_text(encoding="utf-8")
header = english[: english.index("  <data ")]
english_keys = re.findall(r'<data name="([^"]+)"', english)

for lang, values in LANGUAGES.items():
    if len(values) != len(KEYS):
        sys.exit(f"{lang}: {len(values)} values for {len(KEYS)} keys")
    translated = dict(zip(KEYS, values))
    translated["AppDisplayName"] = "Sonar Flyout"
    translated["FooterTitle.Text"] = "SteelSeries Sonar"
    missing = [k for k in english_keys if k not in translated]
    if missing:
        sys.exit(f"{lang}: missing {missing}")
    for key, value in translated.items():
        if "{0}" in re.search(rf'<data name="{re.escape(key)}"[^>]*><value>([^<]*)', english).group(1) and "{0}" not in value:
            sys.exit(f"{lang}: {key} lost its {{0}} placeholder")
    body = "".join(f'  <data name="{k}" xml:space="preserve"><value>{escape(translated[k])}</value></data>\n' for k in english_keys)
    out = root / lang / "Resources.resw"
    out.parent.mkdir(exist_ok=True)
    out.write_text(header + body + "</root>\n", encoding="utf-8")
    print(f"{lang}: {len(english_keys)} strings")
