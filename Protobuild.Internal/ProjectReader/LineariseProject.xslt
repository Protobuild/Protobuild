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
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
 
  <xsl:template match="p:None">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
 
  <xsl:template match="p:Content">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
 
  <xsl:template match="p:EmbeddedResource">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
 
  <xsl:template match="p:EmbeddedNativeLibrary">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
 
  <xsl:template match="p:EmbeddedShaderProgram">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
 
  <xsl:template match="p:ShaderProgram">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
 
  <xsl:template match="p:ApplicationDefinition">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
 
  <xsl:template match="p:Page">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
 
  <xsl:template match="p:AppxManifest">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
 
  <xsl:template match="p:BundleResource">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
 
  <xsl:template match="p:InterfaceDefinition">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
 
  <xsl:template match="p:AndroidResource">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
 
  <xsl:template match="p:SplashScreen">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
 
  <xsl:template match="p:Resource">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>

  <xsl:template match="p:XamarinComponentReference">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>

  <xsl:template match="p:ClInclude">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>

  <xsl:template match="p:ClCompile">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:element
          name="Compile"
          namespace="http://schemas.microsoft.com/developer/msbuild/2003">
          <xsl:attribute name="Include">
            <xsl:value-of select="@Include" />
          </xsl:attribute>
          <xsl:copy-of select="./*" />
        </xsl:element>
      </Included>
    </xsl:if>
  </xsl:template>

  <xsl:template match="p:ResourceCompile">
    <xsl:if test="not(./FromIncludeProject)">
      <Included>
        <xsl:copy-of select="." />
      </Included>
    </xsl:if>
  </xsl:template>
  
  <xsl:template match="*"></xsl:template>
  <xsl:template match="text()"></xsl:template>
 
</xsl:stylesheet>
