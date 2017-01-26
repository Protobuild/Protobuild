#!/usr/bin/env groovy
parallel(
  "Windows": {
    node('windows') {
      checkout poll: false, changelog: false, scm: scm
      bat ("Protobuild.exe --upgrade-all")
      bat ('Protobuild.exe --automated-build')
    }
  },
  "Mac": {
    node('mac') {
      checkout poll: false, changelog: false, scm: scm
      sh ("mono Protobuild.exe --upgrade-all")
      sh ("mono Protobuild.exe --automated-build")
    }
  },
  "Linux": {
    node('linux') {
      checkout poll: true, changelog: true, scm: scm
      sh ("mono Protobuild.exe --upgrade-all")
      sh ("mono Protobuild.exe --automated-build")
    }
  }
)