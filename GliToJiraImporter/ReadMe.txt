This program is meant to read in GLI documents and parse them for use in Jira.

Here are the possible fields and an explanation of each:
	-f (File name/Path)
	-j
	-u
	-p
	-i
	-s
	-k The name of jira project
	-t (A number representing the type of the document. Checkoff is 1)


Here is an example of how each field can look:
	-f C:\MForce\depot\depot\CMTools\JiraHelpers\GliToJiraGenerator\Public\<name of gli document>
	-j http://jira.austin.mgam/ 
	-u JiraBot 
	-p Password#1 
	-i Technical Requirement
	-s 100 
	-k <name of jira project>
	-t 1

To run an example XSD file through it, run the app with one of the following links:
	-f ..\..\..\..\GliToJiraImporter.Testing\Public\Australia-New-Zealand.docx -t 1
	-f ..\..\..\..\GliToJiraImporter.Testing\Public\SINGLE-Australia-New-Zealand.docx -t 1
	-f ..\..\..\..\GliToJiraImporter.Testing\Public\SINGLE-MULTIDESC-Australia-New-Zealand.docx -t 1
	-f ..\..\..\..\GliToJiraImporter.Testing\Public\PICTURES-SHORT-Australia-New-Zealand.docx -t 1
	-f ..\..\..\..\GliToJiraImporter.Testing\Public\SPECIALS-Australia-New-Zealand.docx -t 1
	