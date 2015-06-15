<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:msxsl="urn:schemas-microsoft-com:xslt"
  xmlns:user="urn:my-scripts"
  exclude-result-prefixes="xsl msxsl user"
  version="1.0">

  <xsl:output method="xml" indent="no" />

  <msxsl:script language="C#" implements-prefix="user">
    <msxsl:assembly name="System.Core" />
    <msxsl:assembly name="System.Web" />
    <msxsl:using namespace="System" />
    <msxsl:using namespace="System.Web" />
    <![CDATA[
    public string NormalizeXAPName(string origName)
    {
      return origName.Replace('.','_');
    }
    public string GetRelativePath(string from, string to)
    {
      try
      {
        var current = Environment.CurrentDirectory;
        from = System.IO.Path.Combine(current, from.Replace('\\', '/'));
        to = System.IO.Path.Combine(current, to.Replace('\\', '/'));
        return (new Uri(from).MakeRelativeUri(new Uri(to)))
          .ToString().Replace('/', '\\');
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
    }

    public bool ProjectAndServiceIsActive(
      string platformString,
      string includePlatformString,
      string excludePlatformString,
      string serviceString,
      string includeServiceString,
      string excludeServiceString,
      string activePlatform,
      string activeServicesString)
    {
      if (!ProjectIsActive(platformString, includePlatformString, excludePlatformString, activePlatform))
      {
        return false;
      }

      return ServiceIsActive(serviceString, includeServiceString, excludeServiceString, activeServicesString);
    }

    public bool ProjectIsActive(
      string platformString,
      string includePlatformString,
      string excludePlatformString,
      string activePlatform)
    {
      // Choose either <Platforms> or <IncludePlatforms>
      if (string.IsNullOrEmpty(platformString))
      {
        platformString = includePlatformString;
      }

      // If the exclude string is set, then we must check this first.
      if (!string.IsNullOrEmpty(excludePlatformString))
      {
        var excludePlatforms = excludePlatformString.Split(',');
        foreach (var i in excludePlatforms)
        {
          if (i == activePlatform)
          {
            // This platform is excluded.
            return false;
          }
        }
      }

      // If the platform string is empty at this point, then we allow
      // all platforms since there's no whitelist of platforms configured.
      if (string.IsNullOrEmpty(platformString))
      {
        return true;
      }

      // Otherwise ensure the platform is in the include list.
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
      string includeServiceString,
      string excludeServiceString,
      string activeServicesString)
    {
      var activeServices = activeServicesString.Split(',');

      // Choose either <Services> or <IncludeServices>
      if (string.IsNullOrEmpty(serviceString))
      {
        serviceString = includeServiceString;
      }

      // If the exclude string is set, then we must check this first.
      if (!string.IsNullOrEmpty(excludeServiceString))
      {
        var excludeServices = excludeServiceString.Split(',');
        foreach (var i in excludeServices)
        {
          if (System.Linq.Enumerable.Contains(activeServices, i))
          {
            // This service is excluded.
            return false;
          }
        }
      }

      // If the service string is empty at this point, then we allow
      // all services since there's no whitelist of services configured.
      if (string.IsNullOrEmpty(serviceString))
      {
        return true;
      }

      // Otherwise ensure the service is in the include list.
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

    public bool IsTrue(string text)
    {
      return text.ToLower() == "true";
    }

    public bool IsTrueDefault(string text)
    {
      return text.ToLower() != "false";
    }

    public string ReadFile(string path)
    {
      path = path.Replace('/', System.IO.Path.DirectorySeparatorChar);
      path = path.Replace('\\', System.IO.Path.DirectorySeparatorChar);

      using (var reader = new System.IO.StreamReader(path))
      {
        return reader.ReadToEnd();
      }
    }

    public bool HasXamarinMac()
    {
      return System.IO.File.Exists("/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/XamMac.dll");
    }

    public bool CodesignKeyExists()
    {
      var home = System.Environment.GetEnvironmentVariable("HOME");
      if (string.IsNullOrEmpty(home))
      {
        home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
      }
      var path = System.IO.Path.Combine(home, ".codesignkey");
      return System.IO.File.Exists(path);
    }

    public string GetCodesignKey()
    {
      var home = System.Environment.GetEnvironmentVariable("HOME");
      if (string.IsNullOrEmpty(home))
      {
        home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
      }
      var path = System.IO.Path.Combine(home, ".codesignkey");
      using (var reader = new System.IO.StreamReader(path))
      {
        return reader.ReadToEnd().Trim();
      }
    }

    public string CalculateDefines(string addDefines, string removeDefines)
    {
      var addArray = addDefines.Trim(';').Split(';');
      var removeArray = removeDefines.Trim(';').Split(';');

      var list = new System.Collections.Generic.List<string>();
      foreach (var a in addArray)
      {
        if (!list.Contains(a))
        {
          list.Add(a);
        }
      }
      foreach (var r in removeArray)
      {
        if (list.Contains(r))
        {
          list.Remove(r);
        }
      }

      return string.Join(";", list.ToArray());
    }

    public string GetFilename(string name)
    {
      var components = name.Split(new[] { '\\', '/' });
      if (components.Length == 0)
      {
        throw new Exception("No name specified for NativeBinary");
      }
      return components[components.Length - 1];
    }

    public string ProgramFilesx86()
    {
      if (IntPtr.Size == 8 ||
        !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITW6432"))) {
        return System.Environment.GetEnvironmentVariable("ProgramFiles(x86)");
      }
      return System.Environment.GetEnvironmentVariable("ProgramFiles");
    }

    public string DetectPlatformToolsetVersion() 
    {
      // We can't just select a common low version here; we need to pick a
      // toolset version that's installed with Visual Studio.
      var x86programfiles = ProgramFilesx86();
      for (var i = 20; i >= 10; i--) {
        var path = System.IO.Path.Combine(x86programfiles, @"Microsoft Visual Studio " + i + @".0\VC");
        if (System.IO.Directory.Exists(path)) {
          return i.ToString();
        }
      }
      return "10";
    }
    
    public string DetectBuildToolsVersion() 
    {
      var platformTools = DetectPlatformToolsetVersion();
      switch (platformTools) {
        case "10":
          return "4";
        default:
          return platformTools;
      }
    }

    ]]>
  </msxsl:script>

  <xsl:variable
    name="project"
    select="/Input/Projects/Project[@Name=/Input/Generation/ProjectName]" />

  <xsl:template name="configuration-declaration"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <xsl:param name="type" />
    <xsl:param name="debug" />
    <xsl:param name="config" />
    <xsl:param name="platform" />
    <xsl:param name="projectname" />
    <ProjectConfiguration>
      <xsl:attribute name="Include">
        <xsl:value-of select="$config" />
        <xsl:text>|</xsl:text>
        <xsl:value-of select="$platform" />
      </xsl:attribute>
      <Configuration><xsl:value-of select="$config" /></Configuration>
      <Platform><xsl:value-of select="$platform" /></Platform>
    </ProjectConfiguration>
  </xsl:template>

  <xsl:template name="configuration-basic-definition"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <xsl:param name="type" />
    <xsl:param name="debug" />
    <xsl:param name="config" />
    <xsl:param name="platform" />
    <xsl:param name="projectname" />
    <PropertyGroup Label="Configuration">
      <xsl:attribute name="Condition">
        <xsl:text> '$(Configuration)|$(Platform)' == '</xsl:text>
        <xsl:value-of select="$config" />
<xsl:text>|</xsl:text>
<xsl:value-of select="$platform" />
        <xsl:text>' </xsl:text>
      </xsl:attribute>
      <ConfigurationType>DynamicLibrary</ConfigurationType>
      <xsl:if test="$debug = 'true'">
        <UseDebugLibraries>true</UseDebugLibraries>
      </xsl:if>
      <CharacterSet>Unicode</CharacterSet>
      <PlatformToolset>
        <xsl:text>v</xsl:text>
        <xsl:value-of select="user:DetectPlatformToolsetVersion()"/>
        <xsl:text>0</xsl:text>
      </PlatformToolset>
    </PropertyGroup>
  </xsl:template>

  <xsl:template name="configuration-import-definition"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <xsl:param name="type" />
    <xsl:param name="debug" />
    <xsl:param name="config" />
    <xsl:param name="platform" />
    <xsl:param name="projectname" />
    <ImportGroup Label="PropertySheets">
      <xsl:attribute name="Condition">
        <xsl:text> '$(Configuration)|$(Platform)' == '</xsl:text>
        <xsl:value-of select="$config" />
	<xsl:text>|</xsl:text>
	<xsl:value-of select="$platform" />
        <xsl:text>' </xsl:text>
      </xsl:attribute>
      <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props" Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')" Label="LocalAppDataPlatform" />
    </ImportGroup>
  </xsl:template>

  <xsl:template name="configuration-path-definition"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <xsl:param name="type" />
    <xsl:param name="debug" />
    <xsl:param name="config" />
    <xsl:param name="platform" />
    <xsl:param name="projectname" />
    <PropertyGroup>
      <xsl:attribute name="Condition">
        <xsl:text> '$(Configuration)|$(Platform)' == '</xsl:text>
        <xsl:value-of select="$config" />
	<xsl:text>|</xsl:text>
	<xsl:value-of select="$platform" />
        <xsl:text>' </xsl:text>
      </xsl:attribute>
      <LinkIncremental>true</LinkIncremental>
      <xsl:variable name="platform_path">
        <xsl:choose>
          <xsl:when test="$type = 'Website'">
            <xsl:text></xsl:text>
          </xsl:when>
          <!-- 
              IMPORTANT: When modifying this, or adding new options, 
              remember to update AutomaticProjectPackager as well.
          -->
          <xsl:when test="user:IsTrue(/Input/Properties/ProjectSpecificOutputFolder)">
            <xsl:value-of select="$projectname" />
            <xsl:text>\</xsl:text>
            <xsl:value-of select="/Input/Generation/Platform" />
            <xsl:text>\</xsl:text>
            <xsl:value-of select="$platform" />
            <xsl:text>\</xsl:text>
            <xsl:value-of select="$config" />
          </xsl:when>
          <xsl:when test="user:IsTrueDefault(/Input/Properties/PlatformSpecificOutputFolder)">
            <xsl:value-of select="/Input/Generation/Platform" />
      	    <xsl:text>\</xsl:text>
      	    <xsl:value-of select="$platform" />
      	    <xsl:text>\</xsl:text>
      	    <xsl:value-of select="$config" />
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="$config" />
          </xsl:otherwise>
        </xsl:choose>
      </xsl:variable>
      <OutDir><xsl:text>bin\</xsl:text><xsl:copy-of select="$platform_path" /></OutDir>
      <IntDir><xsl:text>obj\</xsl:text><xsl:copy-of select="$platform_path" /></IntDir>
      <IncludePath>$(IncludePath)</IncludePath>
      <LibraryPath>$(LibraryPath)</LibraryPath>
      <ExecutablePath>$(ExecutablePath)</ExecutablePath>
      <ExcludePath>$(ExcludePath)</ExcludePath>
      <PostBuildEventUseInBuild>true</PostBuildEventUseInBuild>
    </PropertyGroup>
  </xsl:template>

  <xsl:template name="configuration-build-definition"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <xsl:param name="type" />
    <xsl:param name="debug" />
    <xsl:param name="config" />
    <xsl:param name="platform" />
    <xsl:param name="projectname" />
    <ItemDefinitionGroup>
      <xsl:attribute name="Condition">
        <xsl:text> '$(Configuration)|$(Platform)' == '</xsl:text>
        <xsl:value-of select="$config" />
	<xsl:text>|</xsl:text>
	<xsl:value-of select="$platform" />
        <xsl:text>' </xsl:text>
      </xsl:attribute>
      <ClCompile>
        <PrecompiledHeader>NotUsing</PrecompiledHeader>
        <WarningLevel>Level4</WarningLevel>
        <xsl:choose>
          <xsl:when test="$debug = 'true'">
            <Optimization>Disabled</Optimization>
          </xsl:when>
          <xsl:otherwise>
            <Optimization>MaxSpeed</Optimization>
            <FunctionLevelLinking>true</FunctionLevelLinking>
            <IntrinsicFunctions>true</IntrinsicFunctions>
          </xsl:otherwise>
        </xsl:choose>
        <PreprocessorDefinitions>
          <xsl:variable name="addDefines">
            <xsl:if test="$debug = 'true'">
              <xsl:text>_DEBUG;</xsl:text>
            </xsl:if>
            <xsl:for-each select="/Input/Services/Service[@Project=/Input/Generation/ProjectName]">
              <xsl:for-each select="./AddDefines/AddDefine">
                <xsl:value-of select="." />
                <xsl:text>;</xsl:text>
              </xsl:for-each>
            </xsl:for-each>
            <xsl:choose>
              <xsl:when test="/Input/Properties/CustomDefinitions">
                <xsl:for-each select="/Input/Properties/CustomDefinitions/Platform">
                  <xsl:if test="/Input/Generation/Platform = ./@Name">
                    <xsl:value-of select="." />
                  </xsl:if>
                </xsl:for-each>
              </xsl:when>
              <xsl:otherwise>
                <xsl:choose>
                  <xsl:when test="/Input/Generation/Platform = 'Windows'">
                    <xsl:text>_WIN32_WINNT=0x0601;WIN32;_WINDOWS</xsl:text>
                  </xsl:when>
                  <xsl:otherwise>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:text>;</xsl:text>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:variable>
          <xsl:variable name="removeDefines">
            <xsl:for-each select="/Input/Services/Service[@Project=/Input/Generation/ProjectName]">
              <xsl:for-each select="./RemoveDefines/RemoveDefine">
                <xsl:value-of select="." />
                <xsl:text>;</xsl:text>
              </xsl:for-each>
            </xsl:for-each>
          </xsl:variable>
          <xsl:value-of select="user:CalculateDefines($addDefines, $removeDefines)" />
          <xsl:text>;%(PreprocessorDefinitions)</xsl:text>
        </PreprocessorDefinitions>
        <AdditionalIncludeDirectories>.\DirectX\XNAMath</AdditionalIncludeDirectories>
        <DisableSpecificWarnings><xsl:value-of select="/Input/Properties/NoWarn" /></DisableSpecificWarnings>
        <PrecompiledHeaderFile></PrecompiledHeaderFile>
        <PrecompiledHeaderOutputFile></PrecompiledHeaderOutputFile>
      </ClCompile>
      <Link>
        <SubSystem>Windows</SubSystem>
        <AdditionalDependencies>
          <xsl:for-each select="$project/References/Reference">
            <xsl:variable name="include-name" select="./@Include" />
            <xsl:if test="
              count(/Input/Projects/Project[@Name=$include-name]) = 0">
              <xsl:if test="
                count(/Input/Projects/ExternalProject[@Name=$include-name]) > 0">

                <xsl:variable name="extern"
                  select="/Input/Projects/ExternalProject[@Name=$include-name]" />

                <xsl:for-each select="$extern/NativeLibraryLink">
                  <xsl:value-of select="@Name" />
                  <xsl:text>.lib;</xsl:text>
                </xsl:for-each>
                <xsl:for-each select="$extern/Platform
                                        [@Type=/Input/Generation/Platform]">
                  <xsl:for-each select="./NativeLibraryLink">
                    <xsl:value-of select="@Name" />
                    <xsl:text>.lib;</xsl:text>
                  </xsl:for-each>
                  <xsl:for-each select="./Service">
                    <xsl:if test="user:ServiceIsActive(
                      ./@Name,
                      '',
                      '',
                      /Input/Services/ActiveServicesNames)">
                      <xsl:for-each select="./NativeLibraryLink">
                        <xsl:value-of select="@Name" />
                        <xsl:text>.lib;</xsl:text>
                      </xsl:for-each>
                    </xsl:if>
                  </xsl:for-each>
                </xsl:for-each>
                <xsl:for-each select="$extern/Service">
                  <xsl:if test="user:ServiceIsActive(
                    ./@Name,
                    '',
                    '',
                    /Input/Services/ActiveServicesNames)">
                    <xsl:for-each select="./NativeLibraryLink">
                      <xsl:value-of select="@Name" />
                      <xsl:text>.lib;</xsl:text>
                    </xsl:for-each>
                  </xsl:if>
                </xsl:for-each>
              </xsl:if>
            </xsl:if>
          </xsl:for-each>
          <xsl:text>%(AdditionalDependencies)</xsl:text>
        </AdditionalDependencies>
        <xsl:choose>
          <xsl:when test="$debug = 'true'">
            <GenerateDebugInformation>true</GenerateDebugInformation>
          </xsl:when>
          <xsl:otherwise>
            <GenerateDebugInformation>false</GenerateDebugInformation>
            <EnableCOMDATFolding>true</EnableCOMDATFolding>
            <OptimizeReferences>true</OptimizeReferences>
          </xsl:otherwise>
        </xsl:choose>
      </Link>
    </ItemDefinitionGroup>
  </xsl:template>

  <xsl:template match="/">

    <xsl:variable name="ToolsVersion">
      <xsl:value-of select="user:DetectBuildToolsVersion()"/>
      <xsl:text>.0</xsl:text>
    </xsl:variable>
    
    <Project
        DefaultTargets="Build"
        xmlns="http://schemas.microsoft.com/developer/msbuild/2003" ToolsVersion="{$ToolsVersion}">

      <ItemGroup Label="ProjectConfigurations">
        <xsl:call-template name="configuration-declaration">
          <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
          <xsl:with-param name="debug">true</xsl:with-param>
          <xsl:with-param name="config">Debug</xsl:with-param>
          <xsl:with-param name="platform">x64</xsl:with-param>
          <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
        </xsl:call-template>
        <xsl:call-template name="configuration-declaration">
          <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
          <xsl:with-param name="debug">false</xsl:with-param>
          <xsl:with-param name="config">Release</xsl:with-param>
          <xsl:with-param name="platform">x64</xsl:with-param>
          <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
        </xsl:call-template>
      </ItemGroup>
      <PropertyGroup Label="Globals">
        <ProjectGuid>{<xsl:value-of select="$project/@Guid" />}</ProjectGuid>
        <Keyword>Win32Proj</Keyword>
        <RootNamespace>
          <xsl:choose>
            <xsl:when test="/Input/Properties/RootNamespace">
              <xsl:value-of select="/Input/Properties/RootNamespace" />
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="$project/@Name" />
            </xsl:otherwise>
          </xsl:choose>
        </RootNamespace>
      </PropertyGroup>
      <Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />
      <xsl:call-template name="configuration-basic-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">true</xsl:with-param>
        <xsl:with-param name="config">Debug</xsl:with-param>
        <xsl:with-param name="platform">x64</xsl:with-param>
        <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
      </xsl:call-template>
      <xsl:call-template name="configuration-basic-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">false</xsl:with-param>
        <xsl:with-param name="config">Release</xsl:with-param>
        <xsl:with-param name="platform">x64</xsl:with-param>
        <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
      </xsl:call-template>
      <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />
      <ImportGroup Label="ExtensionSettings"></ImportGroup>
      <xsl:call-template name="configuration-import-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">false</xsl:with-param>
        <xsl:with-param name="config">Release</xsl:with-param>
        <xsl:with-param name="platform">x64</xsl:with-param>
        <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
      </xsl:call-template>
      <xsl:call-template name="configuration-import-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">false</xsl:with-param>
        <xsl:with-param name="config">Release</xsl:with-param>
        <xsl:with-param name="platform">x64</xsl:with-param>
        <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
      </xsl:call-template>
      <PropertyGroup Label="UserMacros" />
      <xsl:call-template name="configuration-path-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">true</xsl:with-param>
        <xsl:with-param name="config">Debug</xsl:with-param>
        <xsl:with-param name="platform">x64</xsl:with-param>
        <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
      </xsl:call-template>
      <xsl:call-template name="configuration-path-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">false</xsl:with-param>
        <xsl:with-param name="config">Release</xsl:with-param>
        <xsl:with-param name="platform">x64</xsl:with-param>
        <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
      </xsl:call-template>
      <xsl:call-template name="configuration-build-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">true</xsl:with-param>
        <xsl:with-param name="config">Debug</xsl:with-param>
        <xsl:with-param name="platform">x64</xsl:with-param>
        <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
      </xsl:call-template>
      <xsl:call-template name="configuration-build-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">false</xsl:with-param>
        <xsl:with-param name="config">Release</xsl:with-param>
        <xsl:with-param name="platform">x64</xsl:with-param>
        <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
      </xsl:call-template>
      
      <ItemGroup>
        <xsl:for-each select="$project/Files/None">
          <xsl:if test="user:ProjectAndServiceIsActive(
              ./Platforms,
              ./IncludePlatforms,
              ./ExcludePlatforms,
              ./Services,
              ./IncludeServices,
              ./ExcludeServices,
              /Input/Generation/Platform,
              /Input/Services/ActiveServicesNames)">
            <xsl:element
              name="{name()}"
              namespace="http://schemas.microsoft.com/developer/msbuild/2003">
              <xsl:attribute name="Include">
                <xsl:value-of select="@Include" />
              </xsl:attribute>
              <xsl:apply-templates select="node()"/>
            </xsl:element>
          </xsl:if>
        </xsl:for-each>
      </ItemGroup>
      
      <ItemGroup>
        <xsl:for-each select="$project/Files/ClInclude">
          <xsl:if test="user:ProjectAndServiceIsActive(
              ./Platforms,
              ./IncludePlatforms,
              ./ExcludePlatforms,
              ./Services,
              ./IncludeServices,
              ./ExcludeServices,
              /Input/Generation/Platform,
              /Input/Services/ActiveServicesNames)">
            <xsl:element
              name="{name()}"
              namespace="http://schemas.microsoft.com/developer/msbuild/2003">
              <xsl:attribute name="Include">
                <xsl:value-of select="@Include" />
              </xsl:attribute>
              <xsl:apply-templates select="node()"/>
            </xsl:element>
          </xsl:if>
        </xsl:for-each>
      </ItemGroup>
      
      <ItemGroup>
        <xsl:for-each select="$project/Files/ClCompile">
          <xsl:if test="user:ProjectAndServiceIsActive(
              ./Platforms,
              ./IncludePlatforms,
              ./ExcludePlatforms,
              ./Services,
              ./IncludeServices,
              ./ExcludeServices,
              /Input/Generation/Platform,
              /Input/Services/ActiveServicesNames)">
            <xsl:element
              name="{name()}"
              namespace="http://schemas.microsoft.com/developer/msbuild/2003">
              <xsl:attribute name="Include">
                <xsl:value-of select="@Include" />
              </xsl:attribute>
              <xsl:apply-templates select="node()"/>
            </xsl:element>
          </xsl:if>
        </xsl:for-each>
      </ItemGroup>
      
      <ItemGroup>
        <xsl:for-each select="$project/Files/ResourceCompile">
          <xsl:if test="user:ProjectAndServiceIsActive(
              ./Platforms,
              ./IncludePlatforms,
              ./ExcludePlatforms,
              ./Services,
              ./IncludeServices,
              ./ExcludeServices,
              /Input/Generation/Platform,
              /Input/Services/ActiveServicesNames)">
            <xsl:element
              name="{name()}"
              namespace="http://schemas.microsoft.com/developer/msbuild/2003">
              <xsl:attribute name="Include">
                <xsl:value-of select="@Include" />
              </xsl:attribute>
              <xsl:apply-templates select="node()"/>
            </xsl:element>
          </xsl:if>
        </xsl:for-each>
      </ItemGroup>
      
      <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
      <ImportGroup Label="ExtensionTargets"></ImportGroup>
    </Project>

  </xsl:template>

  <xsl:template match="*">
    <xsl:element
      name="{name()}"
      namespace="http://schemas.microsoft.com/developer/msbuild/2003">
      <xsl:apply-templates select="@*|node()"/>
    </xsl:element>
  </xsl:template>
  
</xsl:stylesheet>
