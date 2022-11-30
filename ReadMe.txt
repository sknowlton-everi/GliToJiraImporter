This program is meant to read in company created GLI documents and parse them for use in Jira. 

Here are the expected fields and a brief description of each:
	-f	(File Path)
	-j	(Jira URL)
	-u	(Username)
	-i	(Jira Issue type)
	-s	(Sleeptime between issues)
	-k	(Jira project name)
	-t	(Document Type Index. Checkoff is 1)

Here is an example of how each field can look:
	-f ..\..\..\..\GliToJiraImporter.Testing\Public\TestCheckoffs\<name of gli document>
	-j http://jira.austin.mgam/ 
	-u JiraBot
	-i Technical Requirement
	-s 100 
	-k <name of jira project>
	-t 1

To run an example XSD file through it, run the app with one of the following links:

	-f ..\..\..\..\GliToJiraImporter.Testing\Public\TestCheckoffs\<name of gli document> -j http://jira.austin.mgam/ -u JiraBot -i Technical Requirement -s 100 -k <name of jira project> -t 1
	-f ..\..\..\..\GliToJiraImporter.Testing\Public\TestCheckoffs\<name of gli document> -i Technical Requirement -s 100 -t 1
	-f ..\..\..\..\GliToJiraImporter.Testing\Public\TestCheckoffs\<name of gli document> -j http://ausappatl02u.austin.mgam/ -u (your username) -i Task -s 100 -k STP -t 1

	GLI Document Options:
	Australia-New-Zealand.docx, SINGLE-Australia-New-Zealand.docx, SINGLE-MULTIDESC-Australia-New-Zealand.docx, PICTURES-SHORT-Australia-New-Zealand.docx, SPECIALS-Australia-New-Zealand.docx
	
	-f ..\..\..\..\GliToJiraImporter.Testing\Public\TestCheckoffs\SINGLE-Australia-New-Zealand.docx -j http://jira.austin.mgam/ -u JiraBot -i Technical Requirement -s 100 -k GRE -t 1
	-f ..\..\..\..\GliToJiraImporter.Testing\Public\TestCheckoffs\SINGLE-Australia-New-Zealand.docx -j http://ausappatl02u.austin.mgam/ -u (your username) -i Task -s 100 -k STP -t 1
	-f ..\..\..\..\GliToJiraImporter.Testing\Public\TestCheckoffs\SINGLE-Australia-New-Zealand.docx -j http://localhost:8080/ -u (your username) -i "Test Plan" -s 100 -k SAM -t 1
	-f ..\..\..\..\GliToJiraImporter.Testing\Public\TestCheckoffs\SINGLE-Australia-New-Zealand.docx -j https://gre-team.atlassian.net/rest/api/2 -u (your username) -i "Test Plan" -s 100 -k EGRE -t 1
