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
</xsl:choose>