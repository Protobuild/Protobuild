<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:msxsl="urn:schemas-microsoft-com:xslt"
  xmlns:user="urn:my-scripts"
  exclude-result-prefixes="xsl msxsl user"
  version="1.0">

  <xsl:output method="xml" indent="no" />

  <xsl:variable name="root" select="/"/>
  
  <!-- {GENERATION_FUNCTIONS} -->

  <!-- {ADDITIONAL_GENERATION_FUNCTIONS} -->

  <xsl:variable
    name="project"
    select="$root/Input/Projects/Project[@Name=$root/Input/Generation/ProjectName]" />

  <xsl:variable name="assembly_name">
    <xsl:choose>
      <xsl:when test="$root/Input/Properties/AssemblyName
	        /Platform[@Name=$root/Input/Generation/Platform]">
        <xsl:value-of select="$root/Input/Properties/AssemblyName/Platform[@Name=$root/Input/Generation/Platform]" />
      </xsl:when>
      <xsl:when test="$root/Input/Properties/AssemblyName">
        <xsl:value-of select="$root/Input/Properties/AssemblyName" />
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$root/Input/Projects/Project[@Name=$root/Input/Generation/ProjectName]/@Name" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:variable>
          
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
  
  <xsl:template name="swig_binding_generator"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PreBuildEvent>
      <Command>
        <xsl:text>"</xsl:text>
        <xsl:value-of select="user:GetKnownTool(&quot;SWIG&quot;)"/>
        <xsl:text>" -csharp -dllimport </xsl:text>
        <xsl:value-of select="$assembly_name" />
        <xsl:text> </xsl:text>
        <xsl:if test="user:IsTrue($root/Input/Properties/BindingGeneratorSWIGEnableCPP)">
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
              $root/Input/Generation/Platform,
              $root/Input/Services/ActiveServicesNames)">
            <xsl:if test="user:PathEndsWith(@Include, &quot;.i&quot;)">
              <xsl:value-of select="@Include" />
              <xsl:text> </xsl:text>
            </xsl:if>
          </xsl:if>
        </xsl:for-each>
        <xsl:for-each select="$project/Files/None">
          <xsl:if test="user:PathEndsWith(@Include, &quot;.i&quot;)">
            <xsl:variable name="extra_protobuild_swig_code">
              <xsl:text>
    protected class ProtobuildSWIGCopyHelper
    {
        static ProtobuildSWIGCopyHelper()
        {
            if (System.Environment.Is64BitProcess)
            {
              </xsl:text>
              <xsl:text>System.IO.File.Copy("</xsl:text>
              <xsl:value-of select="$assembly_name"/>
              <xsl:text>64.dll", "</xsl:text>
              <xsl:value-of select="$assembly_name"/>
              <xsl:text>.dll", true);</xsl:text>
              <xsl:text>
            }
            else
            {
              </xsl:text>
              <xsl:text>System.IO.File.Copy("</xsl:text>
              <xsl:value-of select="$assembly_name"/>
              <xsl:text>32.dll", "</xsl:text>
              <xsl:value-of select="$assembly_name"/>
              <xsl:text>.dll", true);</xsl:text>
              <xsl:text>
            }
        }
    }

    protected static ProtobuildSWIGCopyHelper protobuildSWIGCopyHelper = new ProtobuildSWIGCopyHelper();

              </xsl:text>
            </xsl:variable>
            <xsl:variable name="powershell_command">
              <xsl:text>$filename = "</xsl:text>
              <xsl:value-of select="user:StripExtension(@Include)" />
              <xsl:text>PINVOKE.cs</xsl:text>
              <xsl:text>"; $new_code = "</xsl:text>
              <xsl:value-of select="user:ToBase64StringUTF16LE($extra_protobuild_swig_code)"/>
              <xsl:text>";</xsl:text>
              <xsl:text>
<![CDATA[
$new_code = [System.Convert]::FromBase64String($new_code)
$new_code = [System.Text.Encoding]::Unicode.GetString($new_code)
$code = Get-Content -Raw $filename;
$startIndex = $code.IndexOf("protected class SWIGExceptionHelper");
$code = $code.Substring(0, $startIndex) + $new_code + $code.Substring($startIndex);
Set-Content -Path $filename -Value $code;
]]>
              </xsl:text>
            </xsl:variable>
            <xsl:text>&#xa;&#xd;powershell -EncodedCommand </xsl:text>
            <xsl:value-of select="user:ToBase64StringUTF16LE($powershell_command)"/>
          </xsl:if>
        </xsl:for-each>
      </Command>
    </PreBuildEvent>
    <PostBuildEvent>
      <Command>
        <xsl:text>csc -target:library -out:</xsl:text>
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
              $root/Input/Generation/Platform,
              $root/Input/Services/ActiveServicesNames)">
            <xsl:if test="user:PathEndsWith(@Include, &quot;.i&quot;)">
              <xsl:value-of select="user:StripExtension(@Include)" />
              <xsl:text>.cs </xsl:text>
              <xsl:value-of select="user:StripExtension(@Include)" />
              <xsl:text>PINVOKE.cs </xsl:text>
            </xsl:if>
          </xsl:if>
        </xsl:for-each>
      </Command>
    </PostBuildEvent>
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
          $root/Input/Generation/Platform,
          $root/Input/Services/ActiveServicesNames)">
        <xsl:if test="user:PathEndsWith(@Include, &quot;.i&quot;)">
          <ClCompile>
            <xsl:attribute name="Include">
              <xsl:value-of select="user:StripExtension(@Include)" />
              <xsl:text>_wrap.</xsl:text>
              <xsl:choose>
                <xsl:when test="user:IsTrue($root/Input/Properties/BindingGeneratorSWIGEnableCPP)">
                  <xsl:text>cxx</xsl:text>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:text>c</xsl:text>
                </xsl:otherwise>
              </xsl:choose>
            </xsl:attribute>
          </ClCompile>
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
          $root/Input/Generation/Platform,
          $root/Input/Services/ActiveServicesNames)">
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
      <ConfigurationType>
        <xsl:choose>
          <xsl:when test="$project/@Type = 'Console' or $project/@Type = 'GUI' or $project/@Type = 'App'">
            <xsl:text>Application</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>DynamicLibrary</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
      </ConfigurationType>
      <xsl:if test="$debug = 'true'">
        <UseDebugLibraries>true</UseDebugLibraries>
      </xsl:if>
      <CharacterSet>Unicode</CharacterSet>
      <OriginalPath>$(VCTargetsPath)</OriginalPath>
      <OriginalPathTrimmed>$(OriginalPath.Trim('\\'))</OriginalPathTrimmed>
      <OriginalPathTrimmedLength>$(OriginalPathTrimmed.Length)</OriginalPathTrimmedLength>
      <OriginalPathSubstringStart>$([MSBuild]::Subtract($(OriginalPathTrimmedLength), 4))</OriginalPathSubstringStart>
      <OriginalSubstring>$(OriginalPathTrimmed.Substring($(OriginalPathSubstringStart)))</OriginalSubstring>
      <OriginalSubstringLowered>$(OriginalSubstring.ToLowerInvariant())</OriginalSubstringLowered>
      <PlatformToolset>$(OriginalSubstringLowered)</PlatformToolset>
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
          <xsl:when test="user:IsTrue($root/Input/Properties/ProjectSpecificOutputFolder)">
            <xsl:value-of select="$projectname" />
            <xsl:text>\</xsl:text>
            <xsl:value-of select="$root/Input/Generation/Platform" />
            <xsl:text>\</xsl:text>
            <xsl:value-of select="$platform" />
            <xsl:text>\</xsl:text>
            <xsl:value-of select="$config" />
          </xsl:when>
          <xsl:when test="user:IsTrueDefault($root/Input/Properties/PlatformSpecificOutputFolder)">
            <xsl:value-of select="$root/Input/Generation/Platform" />
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
      <TargetName><xsl:value-of select="$assembly_name"/></TargetName>
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
            <xsl:for-each select="$root/Input/Services/Service[@Project=$root/Input/Generation/ProjectName]">
              <xsl:for-each select="./AddDefines/AddDefine">
                <xsl:value-of select="." />
                <xsl:text>;</xsl:text>
              </xsl:for-each>
            </xsl:for-each>
            <xsl:choose>
              <xsl:when test="$root/Input/Properties/CustomDefinitions">
                <xsl:for-each select="$root/Input/Properties/CustomDefinitions/Platform">
                  <xsl:if test="$root/Input/Generation/Platform = ./@Name">
                    <xsl:value-of select="." />
                  </xsl:if>
                </xsl:for-each>
              </xsl:when>
              <xsl:otherwise>
                <xsl:choose>
                  <xsl:when test="$root/Input/Generation/Platform = 'Windows'">
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
            <xsl:for-each select="$root/Input/Services/Service[@Project=$root/Input/Generation/ProjectName]">
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
        <DisableSpecificWarnings><xsl:value-of select="$root/Input/Properties/NoWarn" /></DisableSpecificWarnings>
        <PrecompiledHeaderFile></PrecompiledHeaderFile>
        <PrecompiledHeaderOutputFile></PrecompiledHeaderOutputFile>
      </ClCompile>
      <Link>
        <SubSystem>
          <xsl:choose>
            <xsl:when test="$project/@Type = 'Console'">
              <xsl:text>Console</xsl:text>
            </xsl:when>
            <xsl:when test="$project/@Type = 'GUI' or $project/@Type = 'App'">
              <xsl:text>Windows</xsl:text>
            </xsl:when>
            <xsl:otherwise>
              <xsl:text>Windows</xsl:text>
            </xsl:otherwise>
          </xsl:choose>
        </SubSystem>
        <AdditionalDependencies>
          <xsl:for-each select="$project/References/Reference">
            <xsl:variable name="include-name" select="./@Include" />
            <xsl:if test="
              count($root/Input/Projects/Project[@Name=$include-name]) = 0">
              <xsl:if test="
                count($root/Input/Projects/ExternalProject[@Name=$include-name]) > 0">

                <xsl:variable name="extern"
                  select="$root/Input/Projects/ExternalProject[@Name=$include-name]" />

                <xsl:for-each select="$extern/NativeLibraryLink">
                  <xsl:value-of select="@Name" />
                  <xsl:text>.lib;</xsl:text>
                </xsl:for-each>
                <xsl:for-each select="$extern/Platform
                                        [@Type=$root/Input/Generation/Platform]">
                  <xsl:for-each select="./NativeLibraryLink">
                    <xsl:value-of select="@Name" />
                    <xsl:text>.lib;</xsl:text>
                  </xsl:for-each>
                  <xsl:for-each select="./Service">
                    <xsl:if test="user:ServiceIsActive(
                      ./@Name,
                      '',
                      '',
                      $root/Input/Services/ActiveServicesNames)">
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
                    $root/Input/Services/ActiveServicesNames)">
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
      <xsl:if test="$root/Input/Properties/BindingGenerator = 'SWIG'">
        <xsl:call-template name="swig_binding_generator">
        </xsl:call-template>
      </xsl:if>
    </ItemDefinitionGroup>
  </xsl:template>

  <xsl:template match="/">

    <xsl:variable name="ToolsVersion">
      <xsl:value-of select="user:DetectWindowsCPlusPlusBuildToolsVersion()"/>
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
        <xsl:call-template name="configuration-declaration">
          <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
          <xsl:with-param name="debug">true</xsl:with-param>
          <xsl:with-param name="config">Debug</xsl:with-param>
          <xsl:with-param name="platform">Win32</xsl:with-param>
          <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
        </xsl:call-template>
        <xsl:call-template name="configuration-declaration">
          <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
          <xsl:with-param name="debug">false</xsl:with-param>
          <xsl:with-param name="config">Release</xsl:with-param>
          <xsl:with-param name="platform">Win32</xsl:with-param>
          <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
        </xsl:call-template>
      </ItemGroup>
      <PropertyGroup Label="Globals">
        <ProjectGuid>{<xsl:value-of select="$project/ProjectGuids/Platform[@Name=$root/Input/Generation/Platform]" />}</ProjectGuid>
        <Keyword>Win32Proj</Keyword>
        <RootNamespace>
          <xsl:choose>
            <xsl:when test="$root/Input/Properties/RootNamespace">
              <xsl:value-of select="$root/Input/Properties/RootNamespace" />
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
      <xsl:call-template name="configuration-basic-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">true</xsl:with-param>
        <xsl:with-param name="config">Debug</xsl:with-param>
        <xsl:with-param name="platform">Win32</xsl:with-param>
        <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
      </xsl:call-template>
      <xsl:call-template name="configuration-basic-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">false</xsl:with-param>
        <xsl:with-param name="config">Release</xsl:with-param>
        <xsl:with-param name="platform">Win32</xsl:with-param>
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
      <xsl:call-template name="configuration-import-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">false</xsl:with-param>
        <xsl:with-param name="config">Release</xsl:with-param>
        <xsl:with-param name="platform">Win32</xsl:with-param>
        <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
      </xsl:call-template>
      <xsl:call-template name="configuration-import-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">false</xsl:with-param>
        <xsl:with-param name="config">Release</xsl:with-param>
        <xsl:with-param name="platform">Win32</xsl:with-param>
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
      <xsl:call-template name="configuration-path-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">true</xsl:with-param>
        <xsl:with-param name="config">Debug</xsl:with-param>
        <xsl:with-param name="platform">Win32</xsl:with-param>
        <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
      </xsl:call-template>
      <xsl:call-template name="configuration-path-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">false</xsl:with-param>
        <xsl:with-param name="config">Release</xsl:with-param>
        <xsl:with-param name="platform">Win32</xsl:with-param>
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
      <xsl:call-template name="configuration-build-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">true</xsl:with-param>
        <xsl:with-param name="config">Debug</xsl:with-param>
        <xsl:with-param name="platform">Win32</xsl:with-param>
        <xsl:with-param name="projectname"><xsl:value-of select="$project/@Name" /></xsl:with-param>
      </xsl:call-template>
      <xsl:call-template name="configuration-build-definition">
        <xsl:with-param name="type"><xsl:value-of select="$project/@Type" /></xsl:with-param>
        <xsl:with-param name="debug">false</xsl:with-param>
        <xsl:with-param name="config">Release</xsl:with-param>
        <xsl:with-param name="platform">Win32</xsl:with-param>
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
              $root/Input/Generation/Platform,
              $root/Input/Services/ActiveServicesNames)">
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

        <xsl:if test="$root/Input/Properties/BindingGenerator = 'SWIG'">
          <xsl:call-template name="swig_binding_generator_extras">
          </xsl:call-template>
        </xsl:if>
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
              $root/Input/Generation/Platform,
              $root/Input/Services/ActiveServicesNames)">
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
        <xsl:for-each select="$project/Files/Compile">
          <xsl:if test="user:ProjectAndServiceIsActive(
              ./Platforms,
              ./IncludePlatforms,
              ./ExcludePlatforms,
              ./Services,
              ./IncludeServices,
              ./ExcludeServices,
              $root/Input/Generation/Platform,
              $root/Input/Services/ActiveServicesNames)">
            <xsl:element
              name="ClCompile"
              namespace="http://schemas.microsoft.com/developer/msbuild/2003">
              <xsl:attribute name="Include">
                <xsl:value-of select="@Include" />
              </xsl:attribute>
              <xsl:apply-templates select="node()"/>
            </xsl:element>
          </xsl:if>
        </xsl:for-each>

        <xsl:if test="$root/Input/Properties/BindingGenerator = 'SWIG'">
          <xsl:call-template name="swig_binding_generator_includes">
          </xsl:call-template>
        </xsl:if>
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
              $root/Input/Generation/Platform,
              $root/Input/Services/ActiveServicesNames)">
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
