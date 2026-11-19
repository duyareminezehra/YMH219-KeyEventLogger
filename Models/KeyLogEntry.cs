using System;
using System.Windows.Forms;

namespace YMH219_KeyEventLogger.Models
{
    /// <summary>
    /// Uygulama penceresi odaktayken yakalanan tek bir klavye olayını temsil eden OOP veri modeli.
    /// </summary>
    public class KeyLogEntry
    {
        public int SequenceNumber { get; }
        public DateTime Timestamp { get; }
        public KeyEventType EventType { get; }
        public string KeyName { get; }
        public int KeyCodeValue { get; }
        public string ModifiersText { get; }
        public string CharacterDisplay { get; }

        public KeyLogEntry(
            int sequenceNumber,
            DateTime timestamp,
            KeyEventType eventType,
            Keys keyCode,
            Keys modifiers,
            char? pressedChar = null)
        {
            SequenceNumber = sequenceNumber;
            Timestamp = timestamp;
            EventType = eventType;
            KeyName = keyCode == Keys.None && pressedChar.HasValue
                ? FormatCharName(pressedChar.Value)
                : keyCode.ToString();
            KeyCodeValue = (int)keyCode;
            ModifiersText = modifiers == Keys.None ? "Yok" : modifiers.ToString();
            CharacterDisplay = pressedChar.HasValue ? FormatCharName(pressedChar.Value) : "-";
        }

        private static string FormatCharName(char c)
        {
            return c switch
            {
                ' ' => "[Space]",
                '\r' or '\n' => "[Enter]",
                '\t' => "[Tab]",
                '\b' => "[Backspace]",
                _ when char.IsControl(c) => $"[Ctrl+{(int)c}]",
                _ => $"'{c}'"
            };
        }

        /// <summary>
        /// ListBox üzerinde hizalı gösterim için formatlı metin üretir.
        /// </summary>
        public string ToDisplayString()
        {
            return $"#{SequenceNumber:D4} | [{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] | {EventType,-8} | Tuş: {KeyName,-12} | Kod: {KeyCodeValue,-3} | Mod: {ModifiersText,-10} | Karakter: {CharacterDisplay}";
        }

        /// <summary>
        /// .txt dosyasına kayıt için standart günlük (log) satırı üretir.
        /// </summary>
        public string ToFileLine()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff}\t{EventType}\tKey={KeyName}\tCode={KeyCodeValue}\tModifiers={ModifiersText}\tChar={CharacterDisplay}";
        }

        public override string ToString() => ToDisplayString();
    }
}
