namespace LiftIO
{
    public interface ILiftMergerTestSuite
    {
        void NewEntryWithGuid();

        void NewEntryWithTextIdIgnoresIt();

       
        void NewEntryTakesGivenDates();

       
        void NewEntryNoDatesUsesNow();

       
        void EntryGetsEmptyLexemeForm();


        void NewWritingSystemAlternativeHandled();

       
        void NewEntryGetsLexemeForm();

       
        void TryCompleteEntry();

        void ModifiedDatesRetained();

       
        void ChangedEntryFound();

       
        void UnchangedEntryPruned();

       
        void EntryWithIncomingUnspecifiedModTimeNotPruned();

        void MergingSameEntryLackingGuidId_TwiceFindMatch();
    }
}