<?xml version="1.0" encoding="utf-8" ?>
<!-- IMPORTANT: VS solutions require tabs for indented lines. Do not remove them from this file! -->
<xsl:stylesheet
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:msxsl="urn:schemas-microsoft-com:xslt"
  xmlns:user="urn:my-scripts"
  exclude-result-prefixes="xsl msxsl user"
  version="1.0">
  
  <xsl:output method="text" indent="no" />

  <xsl:variable name="documentroot" select="/"/>
  
  <!-- {GENERATION_FUNCTIONS} -->

  <!-- {ADDITIONAL_GENERATION_FUNCTIONS} -->

  <xsl:template match="/">
		<xsl:text>Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 14
VisualStudioVersion = 14.0.22609.0
MinimumVisualStudioVersion = 10.0.40219.1
</xsl:text>
    <xsl:for-each select="$documentroot/Input/Projects/Project">
      <xsl:sort select="current()/Priority" />
      <xsl:call-template name="project-definition">
        <xsl:with-param name="type" select="current()/Type" />
        <xsl:with-param name="typeguid" select="current()/TypeGuid" />
        <xsl:with-param name="name" select="current()/Name" />
        <xsl:with-param name="guid" select="current()/Guid" />
        <xsl:with-param name="path" select="current()/Path" />
        <xsl:with-param name="language" select="current()/Language" />
        <xsl:with-param name="deps" select="current()/PostProject" />
      </xsl:call-template>
    </xsl:for-each>
    <xsl:for-each select="$documentroot/Input/Projects/Project/Folder[not(.=preceding::*)]">
      <xsl:sort select="current()/Priority" />
      <xsl:if test="text()">
        <xsl:call-template name="project-definition">
          <xsl:with-param name="type" select="'IDEFolder'" />
          <xsl:with-param name="typeguid" select="''" />
          <xsl:with-param name="name" select="text()" />
          <xsl:with-param name="guid" select="user:GenerateGuid(text())" />
          <xsl:with-param name="path" select="text()" />
          <xsl:with-param name="language" select="current()/Language" />
          <xsl:with-param name="deps" select="current()/PostProject" />
        </xsl:call-template>
      </xsl:if>
    </xsl:for-each>
    
    <xsl:choose>
      <xsl:when test="$documentroot/Input/Generation/Platform = 'iOS'">
        <xsl:text>Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|iPhoneSimulator = Debug|iPhoneSimulator
		Release|iPhoneSimulator = Release|iPhoneSimulator
		Debug|iPhone = Debug|iPhone
		Release|iPhone = Release|iPhone
		Ad-Hoc|iPhone = Ad-Hoc|iPhone
		AppStore|iPhone = AppStore|iPhone
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
</xsl:text>
      </xsl:when>
      <xsl:when test="$documentroot/Input/Generation/Platform = 'tvOS'">
        <xsl:text>Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|iPhoneSimulator = Debug|iPhoneSimulator
		Release|iPhoneSimulator = Release|iPhoneSimulator
		Debug|iPhone = Debug|iPhone
		Release|iPhone = Release|iPhone
		Ad-Hoc|iPhone = Ad-Hoc|iPhone
		AppStore|iPhone = AppStore|iPhone
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
</xsl:text>
      </xsl:when>
      <xsl:when test="$documentroot/Input/Generation/Platform = 'WindowsPhone'">
        <xsl:text>Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|x86 = Debug|x86
		Release|x86 = Release|x86
		Debug|ARM = Debug|ARM
		Release|ARM = Release|ARM
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
</xsl:text>
      </xsl:when>
      <xsl:when test="$documentroot/Input/Generation/Platform = 'WindowsPhone81'">
        <xsl:text>Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
</xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <xsl:text>Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
</xsl:text>
      </xsl:otherwise>
    </xsl:choose>
    <xsl:for-each select="$documentroot/Input/Projects/Project">
      <xsl:sort select="current()/Priority" />
      <xsl:call-template name="project-configuration">
        <xsl:with-param name="guid" select="current()/Guid" />
        <xsl:with-param name="root" select="current()" />
        <xsl:with-param name="language" select="current()/Language" />
      </xsl:call-template>
    </xsl:for-each>
    <xsl:text>	EndGlobalSection
	GlobalSection(NestedProjects) = preSolution
</xsl:text>
<!-- HERE -->
    <xsl:for-each select="$documentroot/Input/Projects/Project">
      <xsl:if test="current()/Folder/text()">
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="current()/Guid" />
        <xsl:text>} = {</xsl:text>
        <xsl:value-of select="user:GenerateGuid(current()/Folder)" />
        <xsl:text>}
</xsl:text>
      </xsl:if>
    </xsl:for-each>
    <xsl:text>	EndGlobalSection
EndGlobal
</xsl:text>
  </xsl:template>
  
  <xsl:template name="project-definition">
    <xsl:param name="name" />
    <xsl:param name="type" />
    <xsl:param name="typeguid" />
    <xsl:param name="path" />
    <xsl:param name="guid" />
    <xsl:param name="language" />
    <xsl:param name="deps" />
    <xsl:text>Project("{</xsl:text>
    <xsl:choose>
      <xsl:when test="$type = 'Content'">
        <xsl:text>9344BDBB-3E7F-41FC-A0DD-8665D75EE146</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'IDEFolder'">
        <xsl:text>2150E333-8FDC-42A3-9474-1A3956D46DE8</xsl:text>
      </xsl:when>
      <xsl:when test="$language = 'C++'">
        <xsl:choose>
          <xsl:when test="$documentroot/Input/Generation/HostPlatform = 'Windows'">
            <xsl:text>8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>2857B73E-F847-4B02-9238-064979017E93</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:when test="$type = 'Custom'">
        <xsl:value-of select="$typeguid" />
      </xsl:when>
      <xsl:otherwise>
        <xsl:text>FAE04EC0-301F-11D3-BF4B-00C04F79EFBC</xsl:text>
      </xsl:otherwise>
    </xsl:choose>
    <xsl:text>}") = </xsl:text>
    <xsl:text>"</xsl:text>
    <xsl:value-of select="$name" />
    <xsl:text>", "</xsl:text>
    <xsl:value-of select="$path" />
    <xsl:text>", "{</xsl:text>
    <xsl:value-of select="$guid" />
    <xsl:text>}"
</xsl:text>
		<xsl:text>	ProjectSection(ProjectDependencies) = postProject
</xsl:text>
    <xsl:for-each select="$deps/*">
      <xsl:text>		{</xsl:text><xsl:value-of select="current()/@Guid" /><xsl:text>} = {</xsl:text><xsl:value-of select="current()/@Guid" /><xsl:text>}
</xsl:text>
    </xsl:for-each>
    <xsl:text>	EndProjectSection
</xsl:text>
<xsl:text>EndProject
</xsl:text>
  </xsl:template>
  
  <xsl:template name="project-configuration">
    <xsl:param name="guid" />
    <xsl:param name="root" />
    <xsl:param name="language" />
    <xsl:variable name="adhoc-mapping">
      <xsl:value-of select="$root/ConfigurationMapping[@Old='Ad-Hoc']/@New" />
    </xsl:variable>
    <xsl:variable name="appstore-mapping">
      <xsl:value-of select="$root/ConfigurationMapping[@Old='AppStore']/@New" />
    </xsl:variable>
    <xsl:variable name="debug-mapping">
      <xsl:value-of select="$root/ConfigurationMapping[@Old='Debug']/@New" />
    </xsl:variable>
    <xsl:variable name="release-mapping">
      <xsl:value-of select="$root/ConfigurationMapping[@Old='Release']/@New" />
    </xsl:variable>
    
    <xsl:choose>
      <xsl:when test="$documentroot/Input/Generation/Platform = 'iOS'">
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Ad-Hoc|iPhone.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$adhoc-mapping != ''">
            <xsl:value-of select="$adhoc-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'iOS'">
            <xsl:text>Ad-Hoc|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Ad-Hoc|iPhone.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$adhoc-mapping != ''">
            <xsl:value-of select="$adhoc-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'iOS'">
            <xsl:text>Ad-Hoc|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.AppStore|iPhone.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$appstore-mapping != ''">
            <xsl:value-of select="$appstore-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'iOS'">
            <xsl:text>AppStore|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.AppStore|iPhone.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$appstore-mapping != ''">
            <xsl:value-of select="$appstore-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'iOS'">
            <xsl:text>AppStore|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|iPhone.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'iOS'">
            <xsl:text>Debug|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|iPhone.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'iOS'">
            <xsl:text>Debug|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|iPhoneSimulator.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'iOS'">
            <xsl:text>Debug|iPhoneSimulator</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|iPhoneSimulator.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'iOS'">
            <xsl:text>Debug|iPhoneSimulator</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|iPhone.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'iOS'">
            <xsl:text>Release|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|iPhone.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'iOS'">
            <xsl:text>Release|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|iPhoneSimulator.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'iOS'">
            <xsl:text>Release|iPhoneSimulator</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|iPhoneSimulator.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'iOS'">
            <xsl:text>Release|iPhoneSimulator</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
      </xsl:when>
      <xsl:when test="$documentroot/Input/Generation/Platform = 'tvOS'">
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Ad-Hoc|iPhone.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$adhoc-mapping != ''">
            <xsl:value-of select="$adhoc-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'tvOS'">
            <xsl:text>Ad-Hoc|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Ad-Hoc|iPhone.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$adhoc-mapping != ''">
            <xsl:value-of select="$adhoc-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'tvOS'">
            <xsl:text>Ad-Hoc|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.AppStore|iPhone.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$appstore-mapping != ''">
            <xsl:value-of select="$appstore-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'tvOS'">
            <xsl:text>AppStore|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.AppStore|iPhone.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$appstore-mapping != ''">
            <xsl:value-of select="$appstore-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'tvOS'">
            <xsl:text>AppStore|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|iPhone.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'tvOS'">
            <xsl:text>Debug|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|iPhone.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'tvOS'">
            <xsl:text>Debug|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|iPhoneSimulator.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'tvOS'">
            <xsl:text>Debug|iPhoneSimulator</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|iPhoneSimulator.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'tvOS'">
            <xsl:text>Debug|iPhoneSimulator</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|iPhone.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'tvOS'">
            <xsl:text>Release|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|iPhone.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'tvOS'">
            <xsl:text>Release|iPhone</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|iPhoneSimulator.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'tvOS'">
            <xsl:text>Release|iPhoneSimulator</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|iPhoneSimulator.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'tvOS'">
            <xsl:text>Release|iPhoneSimulator</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
      </xsl:when>
      <xsl:when test="$documentroot/Input/Generation/Platform = 'WindowsPhone'">
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|x86.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'WindowsPhone'">
            <xsl:text>Debug|x86</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|x86.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'WindowsPhone'">
            <xsl:text>Debug|x86</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|x86.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'WindowsPhone'">
            <xsl:text>Release|x86</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|x86.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'WindowsPhone'">
            <xsl:text>Release|x86</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|ARM.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'WindowsPhone'">
            <xsl:text>Debug|ARM</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|ARM.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'WindowsPhone'">
            <xsl:text>Debug|ARM</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|ARM.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'WindowsPhone'">
            <xsl:text>Release|ARM</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|ARM.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:when test="$root/Platform = 'WindowsPhone'">
            <xsl:text>Release|ARM</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
      </xsl:when>
      <xsl:when test="$language = 'C++' and $documentroot/Input/Generation/HostPlatform = 'Windows'">
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|Any CPU.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|x64</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|Any CPU.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|x64</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|Any CPU.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Release|x64</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|Any CPU.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Release|x64</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|Any CPU.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Debug|Any CPU.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$debug-mapping != ''">
            <xsl:value-of select="$debug-mapping" />
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Debug|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|Any CPU.ActiveCfg = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Release|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
        <xsl:text>		{</xsl:text>
        <xsl:value-of select="$guid" />
        <xsl:text>}.Release|Any CPU.Build.0 = </xsl:text>
        <xsl:choose>
          <xsl:when test="$release-mapping != ''">
            <xsl:value-of select="$release-mapping" />
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>Release|Any CPU</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>
</xsl:text>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  <xsl:template match="*">
    <xsl:element 
      name="{name()}" 
      namespace="http://schemas.microsoft.com/developer/msbuild/2003">
      <xsl:apply-templates select="@*|node()"/>
    </xsl:element>
  </xsl:template>
  
</xsl:stylesheet>
