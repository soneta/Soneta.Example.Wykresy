using Soneta.Business;
using Soneta.CRM;
using Soneta.Example.Wykresy;
using Soneta.Kasa;
using Soneta.Types;

[assembly: Worker(typeof(KntRozrachunkiChartExtender))]
[assembly: Worker(typeof(RozrachunekZakresWorker), typeof(RozrachunekIdx))]

namespace Soneta.Example.Wykresy
{
    // Klasa pomocnicza do wyświetlenie struktry wiekowej należności
    public class KntRozrachunkiChartExtender
    {
        [Context(Required=false)]
        public Kontrahent Kontrahent { get; set; }

        // Lista wszystkich nierozliczonych należności
        public View Naleznosci()
        {
            if (Kontrahent == null)
                return null;

            KasaModule kasamodule = KasaModule.GetInstance(Kontrahent);
            var rozrachunkiview = kasamodule.RozrachunkiIdx.Nierozliczone(Kontrahent, FromTo.All, Date.Today);
            rozrachunkiview.Condition &= new FieldCondition.Equal("Typ", TypRozrachunku.Należność);
            return rozrachunkiview;
        }

        // Property wykorzystywane przez kontrolkę Chart, do przekazania "klikanego" elementu
        public object FocusedItem { get; set; }

        // Metoda obsługująca kliknięcie na wykresie. 
        // W FocusedItem jest obiekt reprezentujący kliknięty element na wykresie
        // Ponieważ wykres zawiera dane zagregowane, kliknięty element zawiera obiekt klasy PivotAggregation
        public object OpenFocusedItem()
        {
            Log("FocusedItem", FocusedItem);
            var pivot = FocusedItem as Business.UI.PivotData.PivotAggregation;
            Log("Pivot", pivot);
            if (pivot != null)
            {
                Log("Values", pivot.Objects.Count);
                Log("Condition", pivot.Condition);

            }
            return pivot;
        }
        private void Log(string caption, object value)
        {
            System.Diagnostics.Trace.WriteLine(caption + ": " + (value == null ? "(null)" : value.ToString()), "Wykres");
        }

    }

    // Pomocniczy worker służący do ustalenia "opisowego" przeterminowania należności
    public class RozrachunekZakresWorker
    {
        [Context]
        public RozrachunekIdx RozrachunekIdx { get; set; }

        public string Przeterminowane
        {
            get
            {
                
                if (RozrachunekIdx.Termin + 14 < Date.Today)
                    return "powyżej 14 dni";
                if (RozrachunekIdx.Termin + 7 < Date.Today)
                    return "8 - 14 dni";
                if (RozrachunekIdx.Termin < Date.Today)
                    return "1 - 7 dni";
                return "Bieżące";
            }
        }
    }
}
