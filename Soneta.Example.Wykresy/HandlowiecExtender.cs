using Soneta.Business;
using Soneta.Business.UI;
using Soneta.CRM;
using Soneta.Example.Wykresy;
using Soneta.Handel;
using Soneta.Types;
using Soneta.Zadania;

[assembly: FolderView("Handel/Sprzedaż/Panel handlowca",
    Priority = 13,
    Description = "Przykładowy pulpit handlowca",
    ObjectType = typeof(HandlowiecExtender),
    ObjectPage = "HandlowiecExtender.Ogolne.pageform.xml",
    IconName="crm",
    ReadOnlySession = true,
    ConfigSession = false
)]

namespace Soneta.Example.Wykresy
{
    // Klasa pomocnicza do wyświetlenia folderu Panel Handlowca
    
    public class HandlowiecExtender
    {
        [Context]
        public Context Context { get; set; }

        public Kontrahent Kontrahent
        {
            get
            {
                return Context.Contains(typeof(Kontrahent)) ? Context[typeof(Kontrahent)] as Kontrahent : null;
            }
            set
            {
                Context.Set(value);
                Context.Session.InvokeChanged();
            }
        }

        // Domyślna osoba kontaktowa (pierwsza na liście)
        public KontaktOsoba Osoba
        {
            get
            {
                KontaktOsoba osoba = null;
                if (Kontrahent != null)
                    foreach (KontaktOsoba o in Kontrahent.Osoby)
                    {
                        osoba = o;
                        break;
                    }

                return osoba;
            }
        }

        // Metoda do wyświetlenia kartoteki kontrahenta
        public Kontrahent OtworzKontrahenta()
        {
            return Kontrahent;
        }

        // Metoda dodająca w module handel dokument ZO
        public DokumentHandlowy DodajZamowienie()
        {
            if (Kontrahent == null)
                return null;

            DokumentHandlowy dokument;
            using (Session session1 = Kontrahent.Session.Login.CreateSession(false, false))
            {
                using (ITransaction tran = session1.Logout(true))
                {
                    HandelModule handelmodule = HandelModule.GetInstance(session1);
                    dokument = new DokumentHandlowy();
                    handelmodule.DokHandlowe.AddRow(dokument);
                    dokument.Definicja = handelmodule.DefDokHandlowych.WgSymbolu["ZO"];
                    dokument.Magazyn = handelmodule.Magazyny.Magazyny.Firma;
                    dokument.Kontrahent = session1[Kontrahent] as Kontrahent;
                    tran.Commit();
                }
            }
            return dokument;
        }

        // Metoda dodająca w CRM zdarzenie NOT
        public Zadanie DodajNotatke()
        {
            if (Kontrahent == null)
                return null;

            Zadanie dokument;
            using (Session session1 = Kontrahent.Session.Login.CreateSession(false, false))
            {
                using (ITransaction tran = session1.Logout(true))
                {
                    ZadaniaModule zadaniamodule = ZadaniaModule.GetInstance(session1);
                    dokument = new Zadanie {
                        Definicja = zadaniamodule.DefZadan.WgSymbolu["NOT"]
                    };
                    zadaniamodule.Zadania.AddRow(dokument);
                    dokument.Kontrahent = session1[Kontrahent] as Kontrahent;
                    if (Osoba != null) dokument.Przedstawiciel = session1[Osoba] as KontaktOsoba;
                    dokument.Nazwa = "Notatka z wizyty " + Date.Today.ToString();
                    tran.Commit();
                }
            }
            return dokument;

        }

    }

}
