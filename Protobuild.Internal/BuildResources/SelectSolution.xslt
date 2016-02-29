<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:msxsl="urn:schemas-microsoft-com:xslt"
  xmlns:user="urn:my-scripts"
  exclude-result-prefixes="xsl msxsl user"
  version="1.0">

  <xsl:variable name="root" select="/"/>

  <!-- {GENERATION_FUNCTIONS} -->

  <!-- {ADDITIONAL_GENERATION_FUNCTIONS} -->

  <xsl:template match="/">
    <xsl:variable name="target_platforms">
      <xsl:choose>
        <xsl:when test="not(/Input/Features/HostPlatformGeneration)">
          <Platform><xsl:value-of select="$root/Input/Generation/Platform" /></Platform>
        </xsl:when>
        <xsl:when test="$root/Input/Projects/Project[@PostBuildHook='True']">
          <Platform><xsl:value-of select="$root/Input/Generation/HostPlatform" /></Platform>
          <xsl:if test="$root/Input/Generation/HostPlatform != $root/Input/Generation/Platform">
            <Platform><xsl:value-of select="$root/Input/Generation/Platform" /></Platform>
          </xsl:if>
        </xsl:when>
        <xsl:otherwise>
          <Platform><xsl:value-of select="$root/Input/Generation/Platform" /></Platform>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <Projects>
      <xsl:for-each select="msxsl:node-set($target_platforms)/*">
        <xsl:variable name="platform" select="." />
        <xsl:for-each select="$root/Input/Projects/Project">
          <xsl:if test="user:ProjectIsActive(
              current()/@Platforms,
              $platform)">
            <Project>
              <Type>
                <xsl:value-of select="current()/@Type" />
              </Type>
              <Platform>
                <xsl:value-of select="$platform" />
              </Platform>
              <RawName>
                <xsl:value-of select="current()/@Name" />
              </RawName>
              <Name>
                <xsl:value-of select="concat(
                  current()/@Name,
                  '.',
                  $platform)" />
              </Name>
              <Guid>
                <xsl:value-of select="current()/ProjectGuids/Platform[@Name=$platform]" />
              </Guid>
              <Path>
                <xsl:value-of select="concat(
                  current()/@Path,
                  '\',
                  current()/@Name,
                  '.',
                  $platform,
                  user:GetProjectExtension(current()/@Language, $root/Input/Generation/HostPlatform))" />
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
              <Folder>
                <xsl:value-of select="current()/Properties/IDEFolder"/>
              </Folder>
              <xsl:copy-of select="current()/ConfigurationMapping" />
              <xsl:copy-of select="current()/PostProject" />
            </Project>
          </xsl:if>
        </xsl:for-each>
        <xsl:for-each select="$root/Input/Projects/ExternalProject
                              /Project">
          <Project>
            <Type>External</Type>
            <Platform>
              <xsl:value-of select="$platform" />
            </Platform>
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
                  <xsl:text>50</xsl:text>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:text>9999</xsl:text>
                </xsl:otherwise>
              </xsl:choose>
            </Priority>
            <Folder>
              <xsl:value-of select="current()/@IDEFolder"/>
            </Folder>
            <xsl:copy-of select="current()/ConfigurationMapping" />
            <xsl:copy-of select="current()/PostProject" />
          </Project>
        </xsl:for-each>
        <xsl:for-each select="$root/Input/Projects/ExternalProject
                              /Platform[@Type=$platform]
                              /Project">
          <Project>
            <Type>External</Type>
            <Platform>
              <xsl:value-of select="$platform" />
            </Platform>
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
                  <xsl:text>50</xsl:text>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:text>9999</xsl:text>
                </xsl:otherwise>
              </xsl:choose>
            </Priority>
            <Folder>
              <xsl:value-of select="current()/@IDEFolder"/>
            </Folder>
            
            <xsl:copy-of select="current()/ConfigurationMapping" />
            <xsl:copy-of select="current()/PostProject" />
          </Project>
        </xsl:for-each>
        <xsl:for-each select="$root/Input/Projects/ExternalProject
                              /Platform[@Type=$platform]
                              /Service">
          <xsl:if test="user:ServiceIsActive(
            ./@Name,
            $root/Input/Services/ActiveServicesNames)">
            <xsl:for-each select="./Project">
              <Project>
                <Type>External</Type>
                <Platform>
                  <xsl:value-of select="$platform" />
                </Platform>
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
                      <xsl:text>50</xsl:text>
                    </xsl:when>
                    <xsl:otherwise>
                      <xsl:text>9999</xsl:text>
                    </xsl:otherwise>
                  </xsl:choose>
                </Priority>
                <Folder>
                  <xsl:value-of select="current()/@IDEFolder"/>
                </Folder>
                <xsl:copy-of select="current()/ConfigurationMapping" />
                <xsl:copy-of select="current()/PostProject" />
              </Project>
            </xsl:for-each>
          </xsl:if>
        </xsl:for-each>
        <xsl:for-each select="$root/Input/Projects/ExternalProject
                              /Service">
          <xsl:if test="user:ServiceIsActive(
            ./@Name,
            $root/Input/Services/ActiveServicesNames)">
            <xsl:for-each select="./Project">
              <Project>
                <Type>External</Type>
                <Platform>
                  <xsl:value-of select="$platform" />
                </Platform>
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
                      <xsl:text>50</xsl:text>
                    </xsl:when>
                    <xsl:otherwise>
                      <xsl:text>9999</xsl:text>
                    </xsl:otherwise>
                  </xsl:choose>
                </Priority>
                <Folder>
                  <xsl:value-of select="current()/@IDEFolder"/>
                </Folder>
                <xsl:copy-of select="current()/ConfigurationMapping" />
                <xsl:copy-of select="current()/PostProject" />
              </Project>
            </xsl:for-each>
          </xsl:if>
        </xsl:for-each>
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