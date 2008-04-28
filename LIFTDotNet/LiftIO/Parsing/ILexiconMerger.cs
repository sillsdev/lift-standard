using System;
using System.Collections.Generic;

namespace LiftIO.Parsing
{
    /// <summary>
    /// Use with the LiftParser (but conceivably other drivers). Allows the same parser
    /// to push LIFT data into multiple systems, e.g. WeSay and FLEx.  Also decouples
    /// different versions of the lift-specific parser from the model-specific stuff,
    /// so either can change or have multiple implementations.
    /// 
    /// The name is somewhat misleading; this isn't about merging two lift files. Rather
    /// it is about pushing the output of the parser into some other object graph/database.
    /// If that graph/database isn't initially empty, then an implementation of this
    /// should be "merging" the incoming data into the existing db, hence the current
    /// name.  WeSay, for one, always starts with an empty DB when parsing a LIFT file,
    /// so it never actually does merging with its implementation of this.
    /// But perhaps a better name would be ILiftPusher?
    /// </summary>
    public interface ILexiconMerger<TBase, TEntry, TSense, TExample>
    {
        TEntry GetOrMakeEntry(Extensible info, int order);
        void EntryWasDeleted(Extensible info, DateTime dateDeleted);
        TSense GetOrMakeSense(TEntry entry, Extensible info, string rawXml);
        TSense GetOrMakeSubsense(TSense sense, Extensible info, string rawXml);
        TExample GetOrMakeExample(TSense sense, Extensible info);

        void MergeInLexemeForm(TEntry entry, LiftMultiText contents);
        void MergeInCitationForm(TEntry entry, LiftMultiText contents);
        // These may be stored as a separate object, which can then contain fields or traits.
        // Thus we need the proper object back on which to store these embedded values.
        TBase MergeInPronunciation(TEntry entry, LiftMultiText contents, string rawXml);
        TBase MergeInVariant(TEntry entry, LiftMultiText contents, string rawXml);

        void MergeInGloss(TSense sense, LiftMultiText multiText);
        void MergeInDefinition(TSense sense, LiftMultiText liftMultiText);
        void MergeInPicture(TSense sense, string href, LiftMultiText caption);

        void MergeInExampleForm(TExample example, LiftMultiText multiText);//, string optionalSource);
        void MergeInTranslationForm(TExample example, string type, LiftMultiText multiText, string rawXml);
        void MergeInSource(TExample example, string source);

        void FinishEntry(TEntry entry);

        /// <summary>
        /// Handle LIFT's "field" entity which can be found on any subclass of "extensible"
        /// </summary>
        void MergeInField(TBase extensible, string typeAttribute, DateTime dateCreated, 
                          DateTime dateModified, 
                          LiftMultiText contents, List<Trait> traits/*, todo: annotations */);

        /// <summary>
        /// Handle LIFT's "trait" entity,
        /// which can be found on any subclass of "extensible", on any "field", and as
        /// a subclass of "annotation".
        /// Note, currently (mar 2007), traits inside forms are instead provided through the LiftMultiText
        /// </summary>
        void MergeInTrait(TBase extensible, Trait trait);

        /// <summary>
        /// Handle LIFT's "relation" entity (currently missing several attributes)
        /// </summary>
        void MergeInRelation(TBase extensible, string relationTypeName, string targetId, string rawXml);

        /// <summary>
        /// Handle LIFT's "note" entity. NB: This may be called multiple times (w/ different notes).
        /// </summary>
        void MergeInNote(TBase extensible, string type, LiftMultiText contents);

        /// <summary>
        /// Handle LIFT's "grammatical-info" entity.  Note that this can occur in a "sense" (or
        /// "subsense") or in a "reversal".
        /// </summary>
        /// <param name="extensible"></param>
        /// <param name="val"></param>
        /// <param name="traits"></param>
        void MergeInGrammaticalInfo(TBase senseOrReversal, string val, List<Trait> traits);

        /// <summary>
        /// Handle a "main" element inside a "reversal" element (possibly nested inside multiple
        /// levels of "main").
        /// </summary>
        /// <param name="parent">the owning object, either a sense or a reversal</param>
        /// <param name="contents"></param>
        /// <param name="type"></param>
        /// <returns>the reversal object</returns>
        TBase GetOrMakeParentReversal(TBase parent, LiftMultiText contents, string type);

        /// <summary>
        /// Handle LIFT's "reversal" entity.
        /// </summary>
        /// <param name="sense"></param>
        /// <param name="parent">the parent reversal object, or null if owned by the sense</param>
        /// <param name="contents"></param>
        /// <param name="type"></param>
        /// <returns>the reversal object</returns>
        TBase MergeInReversal(TSense sense, TBase parent, LiftMultiText contents, string type, string rawXml);

        /// <summary>
        /// Handle LIFT's "etymology" entity.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="source"></param>
        /// <param name="form"></param>
        /// <param name="gloss"></param>
        /// <returns>the etymology object</returns>
        TBase MergeInEtymology(TEntry entry, string source, LiftMultiText form, LiftMultiText gloss, string rawXml);

        /// <summary>
        /// Process a range element from the header (possibly from a separate range file).  The
        /// application may totally ignore this information if it so desires...
        /// </summary>
        /// <param name="range"></param>
        /// <param name="id"></param>
        /// <param name="guid"></param>
        /// <param name="parent"></param>
        /// <param name="description"></param>
        /// <param name="label"></param>
        /// <param name="abbrev"></param>
        void ProcessRangeElement(string range, string id, string guid, string parent,
                                 LiftMultiText description, LiftMultiText label, LiftMultiText abbrev);

        /// <summary>
        /// Process a field definition from the header.  The application may totally ignore this
        /// information if it so desires...
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="description"></param>
        void ProcessFieldDefinition(string tag, LiftMultiText description);
    }

//    /// <summary>
//    /// This helps apps that don't want to model everything, but want to round-trip everything
//    /// </summary>
//    /// <typeparam name="TBase"></typeparam>
//    /// <typeparam name="TEntry"></typeparam>
//    /// <typeparam name="TSense"></typeparam>
//    /// <typeparam name="TExample"></typeparam>
//    public interface ILimittedMerger<TBase, TEntry, TSense, TExample>
//    {
//        bool DoesPreferXmlForPhonetic { get;}
//        void MergeInPronunciation(TEntry entry, string xml);
//
//        bool DoesPreferXmlForEtymology { get;}
//        void MergeInEtymology(TEntry entry, string xml);
//    }
}