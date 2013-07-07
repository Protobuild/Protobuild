<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  exclude-result-prefixes="xsl"
  version="1.0">
  
  <xsl:output method="xml" indent="no" />
 
  <xsl:template match="/Project">
    <Results>
      <xsl:apply-templates />
    </Results>
  </xsl:template>
  
  <xsl:template match="/Project/ItemGroup/Reference">
    <Reference>
      <xsl:value-of select="@Include" />
    </Reference>
  </xsl:template>
  
  <xsl:template match="/Project/ItemGroup/ProjectReference">
    <Reference>
      <xsl:value-of select="@Name" />
    </Reference>
  </xsl:template>
 
  <xsl:template match="/Project/ItemGroup/Compile">
    <Included>
      <xsl:copy-of select="." />
    </Included>
  </xsl:template>
 
  <xsl:template match="/Project/ItemGroup/None">
    <Included>
      <xsl:copy-of select="." />
    </Included>
  </xsl:template>
 
  <xsl:template match="/Project/ItemGroup/Content">
    <Included>
      <xsl:copy-of select="." />
    </Included>
  </xsl:template>
 
  <xsl:template match="/Project/ItemGroup/EmbeddedResource">
    <Included>
      <xsl:copy-of select="." />
    </Included>
  </xsl:template>
    
</xsl:stylesheet>
