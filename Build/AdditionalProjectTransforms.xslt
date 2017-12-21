<xsl:if test="$root/Input/Generation/ProjectName = 'Protobuild.Internal'">
  <Target Name="GenerateProtobuildVersion" BeforeTargets="BeforeBuild">
    <Exec>
      <xsl:attribute name="WorkingDirectory">
        <xsl:value-of select="$root/Input/Generation/RootPath" />
        <xsl:value-of select="$project/@Path" />
      </xsl:attribute>
      <xsl:attribute name="Command">
        <xsl:if test="$root/Input/Generation/HostPlatform = 'Linux'">
          <xsl:text>bash ../Build/GenerateVersion.sh</xsl:text>
        </xsl:if>
        <xsl:if test="$root/Input/Generation/HostPlatform = 'MacOS'">
          <xsl:text>bash ../Build/GenerateVersion.sh</xsl:text>
        </xsl:if>
        <xsl:if test="$root/Input/Generation/HostPlatform = 'Windows'">
          <xsl:text>powershell -ExecutionPolicy Bypass ..\Build\GenerateVersion.ps1</xsl:text>
        </xsl:if>
      </xsl:attribute>
    </Exec>
  </Target>
</xsl:if>