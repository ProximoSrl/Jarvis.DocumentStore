##Version 2.0.0

**Major Changes**: Update Mongo driver to 2.x version, and updated NES to version 6.

### 2.2.5

- Fixed bug when you try to import a file with File Queue with invalid char in name.

### 2.2.4

- Fixed script to kill libreoffice

### 2.2.3

- Minor fix and update reference to framework and NEs

### 2.2.2

- Added ability to change parameters for client directly from a job
- Added retry capability on PdfComposer

### 2.2.1

- Fix in PdfComposer

### 2.2.0

- Updated Mongo Driver to version 2.x
- Updated NES to version 6
- Added PdfComposer conversion (ability to compose multiple pdf in one single pdf)
- Minor fixes

###Breaking changes.

No need for a rebuild, but you need to run the upgrade procedures for database descripted in [Manual database Upgrades](/wiki/BreakingChangesDb.md) regarding the update of name of property of queues.

##Version 1.3.x

Version 1.3.x works with old legacy mongo driver, does not work with Mongo and Wired Tiger.