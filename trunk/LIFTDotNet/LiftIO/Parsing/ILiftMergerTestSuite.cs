namespace LiftIO.Parsing
{
    /// <summary>
    /// The idea here was to have a way to help implementers know what new tests are needed as the
    /// parser grows.  WeSay uses it, but it has not been kept up to date, as of April 2008.
    /// </summary>
    public interface ILiftMergerTestSuite
    {
        void NewEntryGetsGuid();

        void NewEntryWithTextIdIgnoresIt();

       
        void NewEntryTakesGivenDates();

       
        void NewEntryNoDatesUsesNow();

       
        void EntryGetsEmptyLexemeForm();


        void NewWritingSystemAlternativeHandled();

       
        void NewEntryGetsLexemeForm();


        void EntryWithChildren();

        void ModifiedDatesRetained();

       
        void ChangedEntryFound();

       
        void UnchangedEntryPruned();

       
        void EntryWithIncomingUnspecifiedModTimeNotPruned();

        void MergingSameEntryLackingGuidId_TwiceFindMatch();
    }
}