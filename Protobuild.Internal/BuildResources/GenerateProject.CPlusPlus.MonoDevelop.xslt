<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:msxsl="urn:schemas-microsoft-com:xslt"
  xmlns:user="urn:my-scripts"
  exclude-result-prefixes="xsl msxsl user"
  version="1.0">

  <xsl:output method="xml" indent="no" />

  {GENERATION_FUNCTIONS}

  {ADDITIONAL_GENERATION_FUNCTIONS}

  <xsl:variable
    name="project"
    select="/Input/Projects/Project[@Name=/Input/Generation/ProjectName]" />

  <xsl:variable name="assembly_name">
    <xsl:choose>
      <xsl:when test="/Input/Properties/AssemblyName
	        /Platform[@Name=/Input/Generation/Platform]">
        <xsl:value-of select="/Input/Properties/AssemblyName/Platform[@Name=/Input/Generation/Platform]" />
      </xsl:when>
      <xsl:when test="/Input/Properties/AssemblyName">
        <xsl:value-of select="/Input/Properties/AssemblyName" />
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="/Input/Projects/Project[@Name=/Input/Generation/ProjectName]/@Name" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:variable>

  <xsl:template name="platform_path">
    <xsl:param name="type" />
    <xsl:param name="projectname" />
    <xsl:param name="platform" />
    <xsl:param name="config" />
    <xsl:param name="project_specific_output_folder" />
    <xsl:param name="platform_specific_output_folder" />
    <xsl:choose>
      <xsl:when test="$type = 'Website'">
        <xsl:text></xsl:text>
      </xsl:when>
      <!-- 
          IMPORTANT: When modifying this, or adding new options, 
          remember to update AutomaticProjectPackager as well.
      -->
      <xsl:when test="user:IsTrue($project_specific_output_folder)">
        <xsl:value-of select="$projectname" />
        <xsl:text>/</xsl:text>
        <xsl:value-of select="/Input/Generation/Platform" />
        <xsl:text>/</xsl:text>
        <xsl:value-of select="$platform" />
        <xsl:text>/</xsl:text>
        <xsl:value-of select="$config" />
      </xsl:when>
      <xsl:when test="user:IsTrueDefault($platform_specific_output_folder)">
        <xsl:value-of select="/Input/Generation/Platform" />
        <xsl:text>/</xsl:text>
        <xsl:value-of select="$platform" />
        <xsl:text>/</xsl:text>
        <xsl:value-of select="$config" />
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$config" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  <xsl:template name="swig_binding_generator"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <CustomCommands>
      <CustomCommands>
        <Command type="BeforeBuild">
          <xsl:attribute name="workingdir">
            <xsl:text>${ProjectDir}</xsl:text>
          </xsl:attribute>
          <xsl:attribute name="command">
            <xsl:text>swig -csharp -dllimport lib</xsl:text>
            <xsl:value-of select="$assembly_name" />
            <xsl:text> </xsl:text>
            <xsl:if test="user:IsTrue(/Input/Properties/BindingGeneratorSWIGEnableCPP)">
              <xsl:text>-c++ </xsl:text>
            </xsl:if>
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
                <xsl:if test="user:PathEndsWith(@Include, &quot;.i&quot;)">
                  <xsl:value-of select="@Include" />
                  <xsl:text> </xsl:text>
                </xsl:if>
              </xsl:if>
            </xsl:for-each>
          </xsl:attribute>
        </Command>
        <Command type="AfterBuild">
          <xsl:attribute name="workingdir">
            <xsl:text>${ProjectDir}</xsl:text>
          </xsl:attribute>
          <xsl:attribute name="command">
            <xsl:text>mcs -target:library -out:</xsl:text>
            <xsl:text>bin/</xsl:text>
            <xsl:value-of select="$project/@Name" />
            <xsl:text>Binding.dll </xsl:text>
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
                <xsl:if test="user:PathEndsWith(@Include, &quot;.i&quot;)">
                  <xsl:value-of select="user:StripExtension(@Include)" />
                  <xsl:text>.cs </xsl:text>
                  <xsl:value-of select="user:StripExtension(@Include)" />
                  <xsl:text>PINVOKE.cs </xsl:text>
                </xsl:if>
              </xsl:if>
            </xsl:for-each>
          </xsl:attribute>
        </Command>
      </CustomCommands>
    </CustomCommands>
  </xsl:template>
  
  <xsl:template name="swig_binding_generator_includes"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
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
        <xsl:if test="user:PathEndsWith(@Include, &quot;.i&quot;)">
          <Compile>
            <xsl:attribute name="Include">
              <xsl:value-of select="user:StripExtension(@Include)" />
              <xsl:text>_wrap.</xsl:text>
              <xsl:choose>
                <xsl:when test="user:IsTrue(/Input/Properties/BindingGeneratorSWIGEnableCPP)">
                  <xsl:text>cxx</xsl:text>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:text>c</xsl:text>
                </xsl:otherwise>
              </xsl:choose>
            </xsl:attribute>
          </Compile>
        </xsl:if>
      </xsl:if>
    </xsl:for-each>
  </xsl:template>
  
  <xsl:template name="swig_binding_generator_extras"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
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
        <xsl:if test="user:PathEndsWith(@Include, &quot;.i&quot;)">
          <None>
            <xsl:attribute name="Include">
              <xsl:value-of select="user:StripExtension(@Include)" />
              <xsl:text>.cs</xsl:text>
            </xsl:attribute>
          </None>
          <None>
            <xsl:attribute name="Include">
              <xsl:value-of select="user:StripExtension(@Include)" />
              <xsl:text>PINVOKE.cs</xsl:text>
            </xsl:attribute>
          </None>
        </xsl:if>
      </xsl:if>
    </xsl:for-each>
  </xsl:template>

  <xsl:template name="configuration"
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
      <OutputName><xsl:copy-of select="$assembly_name" /></OutputName>
      <CompileTarget>
        <xsl:choose>
          <xsl:when test="$project/@Type = 'Console'">
            <xsl:text>Bin</xsl:text>
          </xsl:when>
          <xsl:when test="$project/@Type = 'GUI'">
            <xsl:text>Bin</xsl:text>
          </xsl:when>
          <xsl:when test="$project/@Type = 'App'">
            <xsl:text>Bin</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>SharedLibrary</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
      </CompileTarget>
      <SourceDirectory>.</SourceDirectory>
      <xsl:choose>
        <xsl:when test="$debug = 'true'">
          <DebugSymbols>true</DebugSymbols>
        </xsl:when>
        <xsl:otherwise>
          <OptimizationLevel>3</OptimizationLevel>
        </xsl:otherwise>
      </xsl:choose>
      <xsl:variable name="platform_path">
        <xsl:call-template name="platform_path">
          <xsl:with-param name="type" select="$type" />
          <xsl:with-param name="projectname" select="$projectname" />
          <xsl:with-param name="platform" select="$platform" />
          <xsl:with-param name="config" select="$config" />
          <xsl:with-param name="platform_specific_output_folder" select="/Input/Properties/PlatformSpecificOutputFolder" />
          <xsl:with-param name="project_specific_output_folder" select="/Input/Properties/ProjectSpecificOutputFolder" />
        </xsl:call-template>
      </xsl:variable>
      <OutputPath><xsl:text>bin\</xsl:text><xsl:copy-of select="$platform_path" /></OutputPath>
      <DefineSymbols>
        <xsl:variable name="addDefines">
          <xsl:if test="$debug = 'true'">
            <xsl:text>DEBUG </xsl:text>
          </xsl:if>
          <xsl:for-each select="/Input/Services/Service[@Project=/Input/Generation/ProjectName]">
            <xsl:for-each select="./AddDefines/AddDefine">
              <xsl:value-of select="." />
              <xsl:text> </xsl:text>
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
                <xsl:when test="/Input/Generation/Platform = 'Android'">
                  <xsl:text>PLATFORM_ANDROID</xsl:text>
                </xsl:when>
                <xsl:when test="/Input/Generation/Platform = 'iOS'">
                  <xsl:text>PLATFORM_IOS</xsl:text>
                </xsl:when>
                <xsl:when test="/Input/Generation/Platform = 'Linux'">
                  <xsl:text>PLATFORM_LINUX</xsl:text>
                </xsl:when>
                <xsl:when test="/Input/Generation/Platform = 'MacOS'">
                  <xsl:text>PLATFORM_MACOS</xsl:text>
                </xsl:when>
                <xsl:when test="/Input/Generation/Platform = 'Ouya'">
                  <xsl:text>PLATFORM_OUYA</xsl:text>
                </xsl:when>
                <xsl:when test="/Input/Generation/Platform = 'PSMobile'">
                  <xsl:text>PLATFORM_PSMOBILE</xsl:text>
                </xsl:when>
                <xsl:when test="/Input/Generation/Platform = 'Windows'">
                  <xsl:text>PLATFORM_WINDOWS</xsl:text>
                </xsl:when>
                <xsl:when test="/Input/Generation/Platform = 'Windows8'">
                  <xsl:text>PLATFORM_WINDOWS8</xsl:text>
                </xsl:when>
                <xsl:when test="/Input/Generation/Platform = 'WindowsGL'">
                  <xsl:text>PLATFORM_WINDOWSGL</xsl:text>
                </xsl:when>
                <xsl:when test="/Input/Generation/Platform = 'WindowsPhone'">
                  <xsl:text>PLATFORM_WINDOWSPHONE</xsl:text>
                </xsl:when>
                <xsl:when test="/Input/Generation/Platform = 'WindowsPhone81'">
                  <xsl:text>PLATFORM_WINDOWSPHONE81</xsl:text>
                </xsl:when>
                <xsl:when test="/Input/Generation/Platform = 'Web'">
                  <xsl:text>PLATFORM_WEB</xsl:text>
                </xsl:when>
              </xsl:choose>
              <xsl:text> </xsl:text>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:variable>
        <xsl:variable name="removeDefines">
          <xsl:for-each select="/Input/Services/Service[@Project=/Input/Generation/ProjectName]">
            <xsl:for-each select="./RemoveDefines/RemoveDefine">
              <xsl:value-of select="." />
              <xsl:text> </xsl:text>
            </xsl:for-each>
          </xsl:for-each>
        </xsl:variable>
        <xsl:value-of select="user:CalculateDefines($addDefines, $removeDefines)" />
      </DefineSymbols>
      
      <xsl:if test="/Input/Properties/BindingGenerator = 'SWIG'">
        <xsl:call-template name="swig_binding_generator">
        </xsl:call-template>
      </xsl:if>
    </PropertyGroup>
  </xsl:template>
  
  <xsl:template match="/">

    <xsl:variable name="ToolsVersion">4.0</xsl:variable>
    
    <Project
        DefaultTargets="Build"
        xmlns="http://schemas.microsoft.com/developer/msbuild/2003" ToolsVersion="{$ToolsVersion}">
    
      <PropertyGroup>
        <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
        <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
        <ProjectGuid>{<xsl:value-of select="$project/@Guid" />}</ProjectGuid>
        <Compiler>
          <Compiler ctype="GccCompiler" />
        </Compiler>
        <Language>C</Language>
        <Target>Bin</Target>
      </PropertyGroup>
      
      <xsl:call-template name="configuration">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">true</xsl:with-param>
        <xsl:with-param name="config">Debug</xsl:with-param>
        <xsl:with-param name="platform">AnyCPU</xsl:with-param>
        <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
      </xsl:call-template>
      <xsl:call-template name="configuration">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">false</xsl:with-param>
        <xsl:with-param name="config">Release</xsl:with-param>
        <xsl:with-param name="platform">AnyCPU</xsl:with-param>
        <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
      </xsl:call-template>
      
      <ItemGroup>
        <xsl:for-each select="$project/Files/Compile">
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

        <xsl:if test="/Input/Properties/BindingGenerator = 'SWIG'">
          <xsl:call-template name="swig_binding_generator_includes">
          </xsl:call-template>
        </xsl:if>
      </ItemGroup>
      
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

        <xsl:if test="/Input/Properties/BindingGenerator = 'SWIG'">
          <xsl:call-template name="swig_binding_generator_extras">
          </xsl:call-template>
        </xsl:if>
      </ItemGroup>

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
