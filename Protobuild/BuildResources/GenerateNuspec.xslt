<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:msxsl="urn:schemas-microsoft-com:xslt"
  xmlns:user="urn:my-scripts"
  exclude-result-prefixes="xsl msxsl user"
  version="1.0">
  
  <xsl:output method="xml" indent="no" />
 
  <xsl:template match="/">
  
    <xsl:variable
      name="project"
      select="/Input/Projects/Project[@Name=/Input/Generation/ProjectName]" />
  
    <package>
        <metadata>
            <id><xsl:value-of select="/Input/Generation/ProjectName" /></id>
            <version>1.0</version>
            <authors>James Rhodes</authors>
            <description>NuGet package</description>
        </metadata>
        <files>
            <file src="bin/Debug/*.exe" target="lib/net40" />
            <file src="bin/Debug/*.exe.mdb" target="lib/net40" />
            <file src="bin/Debug/*.exe.pdb" target="lib/net40" />
            <file src="bin/Debug/*.dll" target="lib/net40" />
            <file src="bin/Debug/*.dll.mdb" target="lib/net40" />
            <file src="bin/Debug/*.dll.pdb" target="lib/net40" />
        </files>
    </package>
        
  </xsl:template>
  
  <xsl:template match="*">
    <xsl:element 
      name="{name()}" 
      namespace="http://schemas.microsoft.com/developer/msbuild/2003">
      <xsl:apply-templates select="@*|node()"/>
    </xsl:element>
  </xsl:template>
  
</xsl:stylesheet>
