<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:msxsl="urn:schemas-microsoft-com:xslt"
  xmlns:user="urn:my-scripts"
  exclude-result-prefixes="xsl msxsl user"
  version="1.0">
 
  <msxsl:script language="C#" implements-prefix="user">
    <msxsl:assembly name="System.Core" />
    <msxsl:assembly name="System.Web" />
    <msxsl:using namespace="System" />
    <msxsl:using namespace="System.Web" />
    <![CDATA[
    public bool ProjectIsActive(string platformString, string activePlatform)
    {
      if (string.IsNullOrEmpty(platformString))
      {
        return true;
      }
      var platforms = platformString.Split(',');
      foreach (var i in platforms)
      {
        if (i == activePlatform)
        {
          return true;
        }
      }
      return false;
    }

    public bool ServiceIsActive(
      string serviceString,
      string activeServicesString)
    {
      if (string.IsNullOrEmpty(serviceString))
      {
        return true;
      }
      var activeServices = activeServicesString.Split(',');
      var services = serviceString.Split(',');
      foreach (var i in services)
      {
        if (System.Linq.Enumerable.Contains(activeServices, i))
        {
          return true;
        }
      }

      return false;
    }

    ]]>
  </msxsl:script>

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
                '.csproj')" />
            </Path>
            <xsl:copy-of select="current()/ConfigurationMapping" />
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