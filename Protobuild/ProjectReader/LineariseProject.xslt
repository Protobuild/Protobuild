<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:p="http://schemas.microsoft.com/developer/msbuild/2003"
  xmlns="http://schemas.microsoft.com/developer/msbuild/2003"
  exclude-result-prefixes="xsl p"
  version="1.0">
  
  <xsl:output method="xml" indent="no" />
 
  <xsl:template match="/">
    <Results>
      <xsl:apply-templates />
    </Results>
  </xsl:template>
  
  <xsl:template match="p:Project"><xsl:apply-templates /></xsl:template>
  <xsl:template match="p:ItemGroup"><xsl:apply-templates /></xsl:template>
  
  <xsl:template match="p:Reference">
    <Reference>
      <xsl:value-of select="@Include" />
    </Reference>
  </xsl:template>
  
  <xsl:template match="p:ProjectReference">
    <Reference>
      <xsl:value-of select="@Name" />
    </Reference>
  </xsl:template>
 
  <xsl:template match="p:Compile">
    <Included>
      <xsl:copy-of select="." />
    </Included>
  </xsl:template>
 
  <xsl:template match="p:None">
    <Included>
      <xsl:copy-of select="." />
    </Included>
  </xsl:template>
 
  <xsl:template match="p:Content">
    <Included>
      <xsl:copy-of select="." />
    </Included>
  </xsl:template>
 
  <xsl:template match="p:EmbeddedResource">
    <Included>
      <xsl:copy-of select="." />
    </Included>
  </xsl:template>
  
  <xsl:template match="*"></xsl:template>
  <xsl:template match="text()"></xsl:template>
 
</xsl:stylesheet>
