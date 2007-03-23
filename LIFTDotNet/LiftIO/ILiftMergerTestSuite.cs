namespace LiftIO
{
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