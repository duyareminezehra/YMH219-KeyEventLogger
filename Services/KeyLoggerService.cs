using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using YMH219_KeyEventLogger.Models;

namespace YMH219_KeyEventLogger.Services
{
    /// <summary>
    /// Klavye olaylarının kaydını, filtrelenmesini, istatistiklerinin hesaplanmasını ve
    /// StreamWriter ile .txt dosyasına aktarılmasını yöneten OOP servis sınıfı.
    /// Yalnızca uygulama formu aktifken gönderilen olayları işler.
    /// </summary>
    public class KeyLoggerService
    {
        private readonly List<KeyLogEntry> _entries = new List<KeyLogEntry>();
        private int _sequenceCounter = 0;

        public bool IsRecording { get; private set; } = false;
        public bool CaptureKeyUp { get; set; } = true;
        public bool CaptureKeyPress { get; set; } = true;

        public IReadOnlyList<KeyLogEntry> AllEntries => _entries;
        public int TotalEventCount => _entries.Count;
        public int TotalKeyDownCount => _entries.Count(e => e.EventType == KeyEventType.KeyDown);
        public int UniqueKeyCount => _entries
            .Where(e => e.EventType == KeyEventType.KeyDown)
            .Select(e => e.KeyName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        public event EventHandler<KeyLogEntry>? EntryAdded;
        public event EventHandler? RecordingStateChanged;
        public event EventHandler? LogsCleared;

        public void StartRecording()
        {
            if (!IsRecording)
            {
                IsRecording = true;
                RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void StopRecording()
        {
            if (IsRecording)
            {
                IsRecording = false;
                RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ClearLogs()
        {
            _entries.Clear();
            _sequenceCounter = 0;
            LogsCleared?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Pencere odaktayken yakalanan bir klavye olayını listeye ekler.
        /// </summary>
        public KeyLogEntry? RecordKeyEvent(
            KeyEventType eventType,
            Keys keyCode,
            Keys modifiers,
            char? pressedChar = null,
            DateTime? customTimestamp = null)
        {
            if (!IsRecording)
            {
                return null;
            }

            if (eventType == KeyEventType.KeyUp && !CaptureKeyUp)
            {
                return null;
            }

            if (eventType == KeyEventType.KeyPress && !CaptureKeyPress)
            {
                return null;
            }

            _sequenceCounter++;
            var entry = new KeyLogEntry(
                _sequenceCounter,
                customTimestamp ?? DateTime.Now,
                eventType,
                keyCode,
                modifiers,
                pressedChar);

            _entries.Add(entry);
            EntryAdded?.Invoke(this, entry);
            return entry;
        }

        /// <summary>
        /// Kayıtları tarih/saat aralığına ve arama metnine göre filtreler (İsteğe bağlı geliştirme).
        /// </summary>
        public List<KeyLogEntry> GetFilteredEntries(
            DateTime? fromTime = null,
            DateTime? toTime = null,
            string? searchKeyText = null)
        {
            IEnumerable<KeyLogEntry> query = _entries;

            if (fromTime.HasValue)
            {
                query = query.Where(e => e.Timestamp >= fromTime.Value);
            }

            if (toTime.HasValue)
            {
                query = query.Where(e => e.Timestamp <= toTime.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchKeyText))
            {
                string filter = searchKeyText.Trim();
                query = query.Where(e =>
                    e.KeyName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    e.CharacterDisplay.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    e.EventType.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            return query.ToList();
        }

        /// <summary>
        /// En sık basılan (KeyDown) tuşların istatistiğini döndürür (İsteğe bağlı geliştirme).
        /// </summary>
        public List<KeyUsageStatistic> GetTopUsedKeys(int maxCount = 6)
        {
            var keyDownEvents = _entries.Where(e => e.EventType == KeyEventType.KeyDown).ToList();
            int total = keyDownEvents.Count;
            if (total == 0)
            {
                return new List<KeyUsageStatistic>();
            }

            return keyDownEvents
                .GroupBy(e => e.KeyName)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key)
                .Take(maxCount)
                .Select(g => new KeyUsageStatistic(
                    g.Key,
                    g.Count(),
                    (g.Count() * 100.0) / total))
                .ToList();
        }

        /// <summary>
        /// Kayıtları StreamWriter kullanarak .txt dosyasına güvenli biçimde yazar.
        /// </summary>
        public void ExportToTextFile(string filePath, IReadOnlyCollection<KeyLogEntry> entriesToSave)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Geçerli bir dosya yolu belirtilmelidir.", nameof(filePath));
            }

            using var writer = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8);
            writer.WriteLine("================================================================================");
            writer.WriteLine("YMH219 - EĞİTSEL KEY EVENT LOGGER KAYIT RAPORU");
            writer.WriteLine($"Oluşturulma Zamanı : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Toplam Olay Sayısı : {TotalEventCount} (KeyDown: {TotalKeyDownCount}, Farklı Tuş: {UniqueKeyCount})");
            writer.WriteLine($"Aktarılan Kayıt    : {entriesToSave.Count}");
            writer.WriteLine("Not: Bu kayıtlar yalnızca uygulama formu odaktayken eğitsel amaçla yakalanmıştır.");
            writer.WriteLine("================================================================================");
            writer.WriteLine();

            var topKeys = GetTopUsedKeys(5);
            if (topKeys.Count > 0)
            {
                writer.WriteLine("--- EN SIK KULLANILAN TUŞ İSTATİSTİKLERİ ---");
                foreach (var stat in topKeys)
                {
                    writer.WriteLine($"  * {stat}");
                }
                writer.WriteLine();
            }

            writer.WriteLine("--- OLAY KAYIT LİSTESİ ---");
            foreach (KeyLogEntry entry in entriesToSave)
            {
                writer.WriteLine(entry.ToFileLine());
            }
        }

        /// <summary>
        /// Ekran görüntüsü ve doğrulama için örnek klavye olayları yükler.
        /// </summary>
        public void LoadSampleDemoData(bool activeState)
        {
            ClearLogs();
            IsRecording = true;

            DateTime baseTime = DateTime.Now.AddMinutes(-2);
            (Keys key, char? ch, Keys mod)[] sampleKeys =
            {
                (Keys.LShiftKey, null, Keys.Shift),
                (Keys.M, 'M', Keys.Shift),
                (Keys.E, 'e', Keys.None),
                (Keys.R, 'r', Keys.None),
                (Keys.H, 'h', Keys.None),
                (Keys.A, 'a', Keys.None),
                (Keys.B, 'b', Keys.None),
                (Keys.A, 'a', Keys.None),
                (Keys.Space, ' ', Keys.None),
                (Keys.N, 'n', Keys.None),
                (Keys.T, 't', Keys.None),
                (Keys.P, 'p', Keys.None),
                (Keys.Space, ' ', Keys.None),
                (Keys.D2, '2', Keys.None),
                (Keys.D0, '0', Keys.None),
                (Keys.D2, '2', Keys.None),
                (Keys.D6, '6', Keys.None),
                (Keys.Return, '\r', Keys.None)
            };

            int offsetMs = 0;
            foreach (var item in sampleKeys)
            {
                DateTime tDown = baseTime.AddMilliseconds(offsetMs);
                RecordKeyEvent(KeyEventType.KeyDown, item.key, item.mod, item.ch, tDown);
                if (item.ch.HasValue)
                {
                    RecordKeyEvent(KeyEventType.KeyPress, item.key, item.mod, item.ch, tDown.AddMilliseconds(18));
                }
                RecordKeyEvent(KeyEventType.KeyUp, item.key, Keys.None, null, tDown.AddMilliseconds(65));
                offsetMs += 210;
            }

            IsRecording = activeState;
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
