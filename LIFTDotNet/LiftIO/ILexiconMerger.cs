using System;

namespace LiftIO
{
    /// <summary>
    /// Use with the LiftParser (but concievably other drivers). Allows the same parser
    /// to push LIFT data into multiple systems, e.g. WeSay and FLEx.  Also decoouples
    /// different versions of the lift-specific parser from the model-specific stuff,
    /// so either can change or have multiple implementations.
    /// </summary>
    public interface ILexiconMerger<TBase, TEntry, TSense, TExample>
    {
        TEntry GetOrMakeEntry(Extensible info);
        TEntry EntryWasDeleted(Extensible info, DateTime dateDeleted);
        TSense GetOrMakeSense(TEntry entry, Extensible info);
        TExample GetOrMakeExample(TSense sense, Extensible info);
        void MergeInLexemeForm(TEntry entry, LiftMultiText contents);
        void MergeInGloss(TSense sense, LiftMultiText multiText);
        void MergeInExampleForm(TExample example, LiftMultiText multiText);//, string optionalSource);
        void MergeInTranslationForm(TExample example, LiftMultiText multiText);
        void MergeInDefinition(TSense sense, LiftMultiText liftMultiText);

        void FinishEntry(TEntry entry);

        /// <summary>
        /// Handle LIFT's "field" entity which can be found on any subclass of "extensible"
        /// </summary>
        /// 
       ///review: a field can also have @value? how does that compare to multitext
        ///todo: field also has @date
        void MergeInField(TBase extensible, string tagAttribute, DateTime dateCreated, 
            DateTime dateModified, 
            LiftMultiText contents);

        /// <summary>
        /// Handle LIFT's "trait" entity,
        /// which can be found on any subclass of "extensible", on any "field", and as
        /// a subclass of "annotation".
        /// </summary>
        void MergeInTrait(TBase extensible, string name, string valueAttribution);

        /// <summary>
        /// Handle LIFT's "note" entity. NB: This may be called multiple times (w/ different notes).
        /// </summary>
        void MergeInNote(TBase extensible, string type, LiftMultiText contents);

        void MergeInGrammaticalInfo(TSense sense, string val);
    }

}