<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:msxsl="urn:schemas-microsoft-com:xslt"
  xmlns:user="urn:my-scripts"
  exclude-result-prefixes="xsl msxsl user"
  version="1.0">

  {GENERATION_FUNCTIONS}

  {ADDITIONAL_GENERATION_FUNCTIONS}

  <xsl:template match="/">
    <Projects>
      <xsl:for-each select="/Input/Projects/Project">
        <xsl:if test="user:ProjectIsActive(
            current()/@Platforms,
            /Input/Generation/Platform)">
          <Project>
            <Type>
              <xsl:value-of select="current()/@Type" />
            </Type>
            <RawName>
              <xsl:value-of select="current()/@Name" />
            </RawName>
            <Name>
              <xsl:value-of select="concat(
                current()/@Name,
                '.',
                /Input/Generation/Platform)" />
            </Name>
            <Guid>
              <xsl:value-of select="current()/@Guid" />
            </Guid>
            <Path>
              <xsl:value-of select="concat(
                current()/@Path,
                '\',
                current()/@Name,
                '.',
                /Input/Generation/Platform,
                user:GetProjectExtension(current()/@Language, /Input/Generation/HostPlatform))" />
            </Path>
            <Language>
              <xsl:choose>
                <xsl:when test="current()/@Language">
                  <xsl:value-of select="current()/@Language" />
                </xsl:when>
                <xsl:otherwise>
                  <xsl:text>C#</xsl:text>
                </xsl:otherwise>
              </xsl:choose>
            </Language>
            <Priority>
              <xsl:choose>
                <xsl:when test="current()/@Language = 'C++'">
                  <!-- C++ projects must come first because they need to build first under MonoDevelop -->
                  <xsl:text>100</xsl:text>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:text>200</xsl:text>
                </xsl:otherwise>
              </xsl:choose>
            </Priority>
            <xsl:copy-of select="current()/ConfigurationMapping" />
            <xsl:copy-of select="current()/PostProject" />
          </Project>
        </xsl:if>
      </xsl:for-each>
      <xsl:for-each select="/Input/Projects/ExternalProject
                            /Project">
        <Project>
          <Type>External</Type>
          <RawName>
            <xsl:value-of select="current()/@Name" />
          </RawName>
          <Name>
            <xsl:value-of select="current()/@Name" />
          </Name>
          <Guid>
            <xsl:value-of select="current()/@Guid" />
          </Guid>
          <Path>
            <xsl:value-of select="current()/@Path" />
          </Path>
          <Priority>9999</Priority>
          <xsl:copy-of select="current()/ConfigurationMapping" />
          <xsl:copy-of select="current()/PostProject" />
        </Project>
      </xsl:for-each>
      <xsl:for-each select="/Input/Projects/ExternalProject
                            /Platform[@Type=/Input/Generation/Platform]
                            /Project">
        <Project>
          <Type>External</Type>
          <RawName>
            <xsl:value-of select="current()/@Name" />
          </RawName>
          <Name>
            <xsl:value-of select="current()/@Name" />
          </Name>
          <Guid>
            <xsl:value-of select="current()/@Guid" />
          </Guid>
          <Path>
            <xsl:value-of select="current()/@Path" />
          </Path>
          <Priority>9999</Priority>
          <xsl:copy-of select="current()/ConfigurationMapping" />
          <xsl:copy-of select="current()/PostProject" />
        </Project>
      </xsl:for-each>
      <xsl:for-each select="/Input/Projects/ExternalProject
                            /Platform[@Type=/Input/Generation/Platform]
                            /Service">
        <xsl:if test="user:ServiceIsActive(
          ./@Name,
          /Input/Services/ActiveServicesNames)">
          <xsl:for-each select="./Project">
            <Project>
              <Type>External</Type>
              <RawName>
                <xsl:value-of select="current()/@Name" />
              </RawName>
              <Name>
                <xsl:value-of select="current()/@Name" />
              </Name>
              <Guid>
                <xsl:value-of select="current()/@Guid" />
              </Guid>
              <Path>
                <xsl:value-of select="current()/@Path" />
              </Path>
              <Priority>9999</Priority>
              <xsl:copy-of select="current()/ConfigurationMapping" />
              <xsl:copy-of select="current()/PostProject" />
            </Project>
          </xsl:for-each>
        </xsl:if>
      </xsl:for-each>
      <xsl:for-each select="/Input/Projects/ExternalProject
                            /Service">
        <xsl:if test="user:ServiceIsActive(
          ./@Name,
          /Input/Services/ActiveServicesNames)">
          <xsl:for-each select="./Project">
            <Project>
              <Type>External</Type>
              <RawName>
                <xsl:value-of select="current()/@Name" />
              </RawName>
              <Name>
                <xsl:value-of select="current()/@Name" />
              </Name>
              <Guid>
                <xsl:value-of select="current()/@Guid" />
              </Guid>
              <Path>
                <xsl:value-of select="current()/@Path" />
              </Path>
              <Priority>9999</Priority>
              <xsl:copy-of select="current()/ConfigurationMapping" />
              <xsl:copy-of select="current()/PostProject" />
            </Project>
          </xsl:for-each>
        </xsl:if>
      </xsl:for-each>
    </Projects>
  </xsl:template>
  
  <xsl:template match="*">
    <xsl:element 
      name="{name()}" 
      namespace="http://schemas.microsoft.com/developer/msbuild/2003">
      <xsl:apply-templates select="@*|node()"/>
    </xsl:element>
  </xsl:template>
  
</xsl:stylesheet>