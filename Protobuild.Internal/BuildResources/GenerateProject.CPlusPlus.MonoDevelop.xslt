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
        <xsl:value-of select="$root/Input/Generation/Platform" />
        <xsl:text>/</xsl:text>
        <xsl:value-of select="$platform" />
        <xsl:text>/</xsl:text>
        <xsl:value-of select="$config" />
      </xsl:when>
      <xsl:when test="user:IsTrueDefault($platform_specific_output_folder)">
        <xsl:value-of select="$root/Input/Generation/Platform" />
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
          $root/Input/Generation/Platform,
          $root/Input/Services/ActiveServicesNames)">
        <xsl:if test="user:PathEndsWith(@Include, &quot;.i&quot;)">
          <Compile>
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
          <xsl:with-param name="platform_specific_output_folder" select="$root/Input/Properties/PlatformSpecificOutputFolder" />
          <xsl:with-param name="project_specific_output_folder" select="$root/Input/Properties/ProjectSpecificOutputFolder" />
        </xsl:call-template>
      </xsl:variable>
      <OutputPath><xsl:text>bin\</xsl:text><xsl:copy-of select="$platform_path" /></OutputPath>
      <DefineSymbols>
        <xsl:variable name="addDefines">
          <xsl:if test="$debug = 'true'">
            <xsl:text>DEBUG </xsl:text>
          </xsl:if>
          <xsl:for-each select="$root/Input/Services/Service[@Project=$root/Input/Generation/ProjectName]">
            <xsl:for-each select="./AddDefines/AddDefine">
              <xsl:value-of select="." />
              <xsl:text> </xsl:text>
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
                <xsl:when test="$root/Input/Generation/Platform = 'Android'">
                  <xsl:text>PLATFORM_ANDROID</xsl:text>
                </xsl:when>
                <xsl:when test="$root/Input/Generation/Platform = 'iOS'">
                  <xsl:text>PLATFORM_IOS</xsl:text>
                </xsl:when>
                <xsl:when test="$root/Input/Generation/Platform = 'Linux'">
                  <xsl:text>PLATFORM_LINUX</xsl:text>
                </xsl:when>
                <xsl:when test="$root/Input/Generation/Platform = 'MacOS'">
                  <xsl:text>PLATFORM_MACOS</xsl:text>
                </xsl:when>
                <xsl:when test="$root/Input/Generation/Platform = 'Ouya'">
                  <xsl:text>PLATFORM_OUYA</xsl:text>
                </xsl:when>
                <xsl:when test="$root/Input/Generation/Platform = 'PSMobile'">
                  <xsl:text>PLATFORM_PSMOBILE</xsl:text>
                </xsl:when>
                <xsl:when test="$root/Input/Generation/Platform = 'Windows'">
                  <xsl:text>PLATFORM_WINDOWS</xsl:text>
                </xsl:when>
                <xsl:when test="$root/Input/Generation/Platform = 'Windows8'">
                  <xsl:text>PLATFORM_WINDOWS8</xsl:text>
                </xsl:when>
                <xsl:when test="$root/Input/Generation/Platform = 'WindowsGL'">
                  <xsl:text>PLATFORM_WINDOWSGL</xsl:text>
                </xsl:when>
                <xsl:when test="$root/Input/Generation/Platform = 'WindowsPhone'">
                  <xsl:text>PLATFORM_WINDOWSPHONE</xsl:text>
                </xsl:when>
                <xsl:when test="$root/Input/Generation/Platform = 'WindowsPhone81'">
                  <xsl:text>PLATFORM_WINDOWSPHONE81</xsl:text>
                </xsl:when>
                <xsl:when test="$root/Input/Generation/Platform = 'Web'">
                  <xsl:text>PLATFORM_WEB</xsl:text>
                </xsl:when>
              </xsl:choose>
              <xsl:text> </xsl:text>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:variable>
        <xsl:variable name="removeDefines">
          <xsl:for-each select="$root/Input/Services/Service[@Project=$root/Input/Generation/ProjectName]">
            <xsl:for-each select="./RemoveDefines/RemoveDefine">
              <xsl:value-of select="." />
              <xsl:text> </xsl:text>
            </xsl:for-each>
          </xsl:for-each>
        </xsl:variable>
        <xsl:value-of select="user:CalculateDefines($addDefines, $removeDefines)" />
      </DefineSymbols>
      
      <xsl:if test="$root/Input/Properties/BindingGenerator = 'SWIG'">
        <xsl:call-template name="swig_binding_generator">
        </xsl:call-template>
      </xsl:if>
    </PropertyGroup>
  </xsl:template>
  
  <xsl:template name="ReferenceToProtobuildProject"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <xsl:param name="target_project_name" />
    <xsl:param name="source_project_name" />
    
    <xsl:variable
      name="target_project"
      select="$root/Input/Projects/Project[@Name=$target_project_name]" />
    <xsl:variable
      name="source_project"
      select="$root/Input/Projects/Project[@Name=$source_project_name]" />
    
    <xsl:if test="user:ProjectIsActive(
      $target_project/@Platforms,
      '',
      '',
      $root/Input/Generation/Platform)">

      <xsl:choose>
        <xsl:when test="$target_project/@Language = 'C#'">
          <xsl:message terminate="yes">
            C++ projects can not reference C# projects.
          </xsl:message>
        </xsl:when>
        <xsl:when test="$target_project/@Language = 'C++'">
          <Package IsProject="True">
            <xsl:attribute name="file">
              <xsl:value-of
                select="user:GetRelativePath(
                  $root/Input/Generation/WorkingDirectory,
                  concat(
                    $source_project/@Path,
                    '\',
                    $source_project/@Name,
                    '.',
                    $root/Input/Generation/Platform,
                    '.srcproj'),
                  concat(
                    $target_project/@Path,
                    '\',
                    $target_project/@Name,
                    '.',
                    $root/Input/Generation/Platform,
                    '.md.pc'))" />
            </xsl:attribute>
            <xsl:attribute name="file">
              <xsl:value-of select="$target_project/@Name" />
            </xsl:attribute>
          </Package>
        </xsl:when>
        <xsl:otherwise>
          <xsl:message terminate="yes">
            <xsl:text>The project </xsl:text>
            <xsl:value-of select="$target_project_name" />
            <xsl:text>does not have a known language (it was '</xsl:text>
            <xsl:value-of select="$target_project/@Language" />
            <xsl:text>').</xsl:text>
          </xsl:message>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:if>
  </xsl:template>
  
  <xsl:template match="/">

    <xsl:variable name="ToolsVersion">4.0</xsl:variable>
    
    <Project
        DefaultTargets="Build"
        xmlns="http://schemas.microsoft.com/developer/msbuild/2003" ToolsVersion="{$ToolsVersion}">
    
      <PropertyGroup>
        <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
        <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
        <ProjectGuid>{<xsl:value-of select="$project/ProjectGuids/Platform[@Name=$root/Input/Generation/Platform]" />}</ProjectGuid>
        <Compiler>
          <Compiler ctype="GccCompiler" />
        </Compiler>
        <Language>C</Language>
        <Target>Bin</Target>
        <Packages>
          <Packages>
            <xsl:for-each select="$project/References/Reference">
              <xsl:variable name="include-name" select="./@Include" />
              <xsl:if test="
                count($root/Input/Projects/Project[@Name=$include-name]) = 0">
                <xsl:if test="
                  count($root/Input/Projects/ExternalProject[@Name=$include-name]) > 0">

                  <xsl:variable name="extern"
                    select="$root/Input/Projects/ExternalProject[@Name=$include-name]" />

                  <xsl:for-each select="$extern/Project">
                    <!-- Ignore this tag for now -->
                  </xsl:for-each>

                  <xsl:for-each select="$extern/Platform
                                          [@Type=$root/Input/Generation/Platform]">
                    <xsl:for-each select="./Project">
                      <!-- Ignore this tag for now -->
                    </xsl:for-each>
                    <xsl:for-each select="./Service">
                      <xsl:if test="user:ServiceIsActive(
                        ./@Name,
                        '',
                        '',
                        $root/Input/Services/ActiveServicesNames)">
                        <xsl:for-each select="./Project">
                          <!-- Ignore this tag for now -->
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
                      <xsl:for-each select="./Project">
                        <!-- Ignore this tag for now -->
                      </xsl:for-each>
                    </xsl:if>
                  </xsl:for-each>

                  <xsl:for-each select="$extern/Reference">
                    <xsl:variable name="refd-name" select="./@Include" />
                    <xsl:if test="count($root/Input/Projects/Project[@Name=$refd-name]) > 0">
                      <xsl:call-template name="ReferenceToProtobuildProject">
                        <xsl:with-param name="target_project_name" select="$refd-name" />
                        <xsl:with-param name="source_project_name" select="$project/@Name" />
                      </xsl:call-template>
                    </xsl:if>
                  </xsl:for-each>


                  <xsl:for-each select="$extern/Platform
                                          [@Type=$root/Input/Generation/Platform]">
                    <xsl:for-each select="./Reference">
                      <xsl:variable name="refd-name" select="./@Include" />
                      <xsl:if test="count($root/Input/Projects/Project[@Name=$refd-name]) > 0">
                        <xsl:call-template name="ReferenceToProtobuildProject">
                          <xsl:with-param name="target_project_name" select="$refd-name" />
                          <xsl:with-param name="source_project_name" select="$project/@Name" />
                        </xsl:call-template>
                      </xsl:if>
                    </xsl:for-each>
                    <xsl:for-each select="./Service">
                      <xsl:if test="user:ServiceIsActive(
                        ./@Name,
                        '',
                        '',
                        $root/Input/Services/ActiveServicesNames)">
                        <xsl:for-each select="./Reference">
                          <xsl:variable name="refd-name" select="./@Include" />
                          <xsl:if test="count($root/Input/Projects/Project[@Name=$refd-name]) > 0">
                            <xsl:call-template name="ReferenceToProtobuildProject">
                              <xsl:with-param name="target_project_name" select="$refd-name" />
                              <xsl:with-param name="source_project_name" select="$project/@Name" />
                            </xsl:call-template>

                          </xsl:if>
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
                      <xsl:for-each select="./Reference">
                        <xsl:variable name="refd-name" select="./@Include" />
                        <xsl:if test="count($root/Input/Projects/Project[@Name=$refd-name]) > 0">
                          <xsl:call-template name="ReferenceToProtobuildProject">
                            <xsl:with-param name="target_project_name" select="$refd-name" />
                            <xsl:with-param name="source_project_name" select="$project/@Name" />
                          </xsl:call-template>
                        </xsl:if>
                      </xsl:for-each>
                    </xsl:if>
                  </xsl:for-each>

                </xsl:if>
              </xsl:if>
            </xsl:for-each>

            <xsl:for-each select="$project/References/Reference">
              <xsl:variable name="include-path" select="./@Include" />
              <xsl:if test="
                count($root/Input/Projects/Project[@Name=$include-path]) > 0">
                <xsl:if test="
                  count($root/Input/Projects/ExternalProject[@Name=$include-path]) = 0">
                  <xsl:call-template name="ReferenceToProtobuildProject">
                    <xsl:with-param name="target_project_name" select="$include-path" />
                    <xsl:with-param name="source_project_name" select="$project/@Name" />
                  </xsl:call-template>
                </xsl:if>
              </xsl:if>
            </xsl:for-each>

          </Packages>
        </Packages>
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
