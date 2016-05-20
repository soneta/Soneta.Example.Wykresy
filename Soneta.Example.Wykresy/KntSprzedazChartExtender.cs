using System;
using System.Collections.Generic;
using Soneta.Business;
using Soneta.CRM;
using Soneta.Example.Wykresy;
using Soneta.Towary;
using Soneta.Types;

[assembly: Worker(typeof(KntSprzedazChartExtender))]

namespace Soneta.Example.Wykresy
{
    #region KlasyItems
    // -----------------------------
    // Klasy pomocnicze dla wyników
    // -----------------------------


    // Element tablicy wynikowej - obroty wg towarów
    public class KontrahentChartItem : IComparable<KontrahentChartItem>
    {
        public Towar Towar { get; set; }
        public decimal Rozchod { get; set; }
        public decimal Marza { get; set; }

        public int CompareTo(KontrahentChartItem other)
        {
            if (other == null) return 1;
            if (Rozchod != other.Rozchod) return Rozchod.CompareTo(other.Rozchod);
            if (Marza != other.Marza) return Marza.CompareTo(other.Marza);
            return Towar.Kod.CompareTo(other.Towar.Kod);
        }
    }

    // Element tablicy wynikowej - obroty wg miesięcy
    public class ObrotyMiesiacItem : IComparable<ObrotyMiesiacItem>
    {
        public YearMonth Miesiac { get; set; }
        public decimal Rozchod { get; set; }
        public decimal Marza { get; set; }

        public int CompareTo(ObrotyMiesiacItem other)
        {
            return other == null ? 1 : Miesiac.CompareTo(other.Miesiac);
        }
    }

    #endregion

    // Klasa extendera wyliczająca źródło dla wykresu sprzedaży wg towarów
    public class KntSprzedazChartExtender
    {
        #region Parametry
        FromTo _okres = FromTo.Month(Date.Today);
        int _top = 5;
        

        [Context(Required=false)]
        public Kontrahent Kontrahent { get; set; }
        
        // Okres
        public FromTo Okres
        {
            get { return _okres; }
            set
            {
                _okres = value;
                Kontrahent?.Session.InvokeChanged();
            }
        }

        // Ile pozycji na wykresie
        public int Top
        {
            get { return _top; }
            set
            {
                _top = value;
                Kontrahent?.Session.InvokeChanged();
            }
        }
        #endregion

        #region ObrotyWgTowarow
        // ------------------------------------
        // Lista obrotów będąca źródłem wykresu
        // ------------------------------------

        // Lista obrotów niezagregowana
        public SubTable ObrotyRazem()
        {
            return ObrotyWgOkresu(Okres);
        }

        public SubTable ObrotyWgOkresu(FromTo okres)
        {
            if (Kontrahent == null)
                return null;

            // Obroty towaru w okresie dla kontrahenta
            SubTable obrotyst = Magazyny.MagazynyModule.GetInstance(Kontrahent).Obroty.WgRozchodKontrahent[Kontrahent];
            if (okres != FromTo.All && okres != FromTo.Empty)
                obrotyst = obrotyst[new FieldCondition.Contain("Data", okres)];
            
            return obrotyst;
        }

        // Lista obrotów - źródło wykresu
        // Wyniki zostają zagregowane dla poszczególnych towarów i wyświetlone jest pierwsze N pozycji (wg największych obrotów)
        public List<KontrahentChartItem> ObrotyWgTowar()
        {
            if (Kontrahent == null)
                return new List<KontrahentChartItem>();

            Dictionary<Towar, KontrahentChartItem> obroty = new Dictionary<Towar, KontrahentChartItem>();

            foreach (Magazyny.Obrot obrot in ObrotyRazem())
            {
                KontrahentChartItem item;
                if (!obroty.TryGetValue(obrot.Towar, out item))
                    obroty[obrot.Towar] = item = new KontrahentChartItem { Towar = obrot.Towar, Rozchod = 0, Marza = 0 };

                item.Rozchod += obrot.Rozchod.Wartosc;
                item.Marza += obrot.Marża;
            }

            List<KontrahentChartItem> lista = new List<KontrahentChartItem>(obroty.Values);

            lista.Sort();
            lista.Reverse();
            
            // Zwrócenie pierwszych n pozycji
            if (lista.Count <= Top)
                return lista;
            else
                return lista.GetRange(0, Top);
        }
        #endregion

        // Property wykorzystywane przez kontrolkę Chart, do przekazania "klikanego" elementu
        public KontrahentChartItem FocusedItem { get; set; }

        // Metoda obsługująca kliknięcie na wykresie. 
        // W FocusedItem jest obiekt reprezentujący kliknięty element na wykresie - w tym wypadku obiekt klasy KontrahentChartItem
        public Towar OpenFocusedItem()
        {
            return FocusedItem.Towar;
        }

        #region ObrotyMiesieczne
        // -------------------------------------------
        // Zestawienie obrotów agregowane miesięcznie
        // Wykorzystane na wykresie sprzedaży rocznej na panelu handlowca
        // -------------------------------------------
        public List<ObrotyMiesiacItem> ObrotyWgMiesiac()
        {
            if (Kontrahent == null)
                return new List<ObrotyMiesiacItem>();


            // Sumowanie ostatnie 12 miesięcy
            Dictionary<YearMonth, ObrotyMiesiacItem> obroty = new Dictionary<YearMonth, ObrotyMiesiacItem>();
            for (int i = -11; i <= 0; i++)
            {
                YearMonth ym = Date.Today.ToYearMonth().AddMonths(i);
                obroty[ym] = new ObrotyMiesiacItem { Miesiac = ym, Rozchod = 0, Marza = 0 };
            }
            foreach (Magazyny.Obrot obrot in ObrotyWgOkresu(new FromTo(Date.Today.AddMonths(-11).FirstDayMonth(), Date.Today.LastDayMonth())))
            {
                ObrotyMiesiacItem item = obroty[obrot.Data.ToYearMonth()];
                item.Rozchod += obrot.Rozchod.Wartosc;
                item.Marza += obrot.Marża;
            }

            List<ObrotyMiesiacItem> lista = new List<ObrotyMiesiacItem>(obroty.Values);
            lista.Sort();
            return lista;
        }
        #endregion
       
    }
}
