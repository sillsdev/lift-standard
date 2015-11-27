This project is the home for the development of an XML schema for the storing of lexical information, as used in the creation of dictionaries.  It also includes a .net library for working with LIFT (parsing, validating, & migrating).

# What is LIFT? #
LIFT (Lexicon Interchange FormaT) is an XML format for storing lexical information, as used in the creation of dictionaries. It's not necessarily _the_ format for your lexicon.  That can be tied to whatever program you're using.  But LIFT allows you to move that data between programs (hence the term 'interchange').

LIFT is also a decent archiving option.  Not because it will be around in 50 years, but because people will still be able to read it with any text editor and easily make use of it, even then. (You think that's true of your non-[SOLID](http://projects.mseag.org/SOLID) Standard Format file? We should have a chat.)

LIFT has been designed to have a long life but also to be relatively easy to convert to and from existing lexicon formats, particularly Multi-Dictionary Formatter (MDF) and [FieldWorks Language Explorer](http://www.sil.org/computing/fieldworks/flex/).

# Programs that support LIFT #

  * [WeSay](http://www.wesay.org) uses LIFT as its primary format. More info [here](http://www.wesay.org/wiki/LIFT).
  * [FieldWorks Language Explorer (FLEx)](http://www.sil.org/computing/fieldworks/flex/) can import and export LIFT files.
  * [Lexique Pro](http://lexiquepro.com) has a growing level of LIFT support. Starting with version 3.2, it can open LIFT documents for viewing, printing, and making web pages.  It can also save to LIFT format.

# Utilities for working with LIFT #

  * [Solid](http://palaso.org/solid) can convert basic SFM to LIFT.
  * [LiftTweaker](http://projects.palaso.org/projects/show/lifttweaker) Selectively modify a LIFT file for targeting different publication types (e.g. proper names, children's, etc.). Also useful for trimming various kinds of cross-entry references to those that you actually want to see on the printed page.
  * [LiftTools](http://projects.palaso.org/projects/show/lifttools).
    * Merge entries which are homographic.
    * Validate Lift File. Ensure the file is compliant with the latest version of the LIFT schema.



# Details #

The LIFT standard is still evolving, as developers work with it.  The most recent version is always available in the [repository](http://lift-standard.googlecode.com/svn/trunk/), as an ODF-format writeup and  RelaxNG schema.  The mailing list is fairly active.