#!groovy​
@Library('SoloPipeline@1.0-rc5')
import com.soloplan.*

pipeline {
  agent {
    label 'dotnet-framework'
  } 

  stages {
    stage('Agent info') {
      steps {
        stepAgentInfo()
      }
    }

    stage('Build') {
      steps {
        stepMSBuild(project: 'src/Soloplan.WhatsON.sln', outputDir: '')
      }
    }

    stage('Publish Snapshot') {
      when {
        branch 'master'
      }
      
      steps {
        stepPublishArtifacts(folder: "src/bin/Release", bucket: "whatson", exclude: [], subfolders: false)
      }
    }

    stage('Publish Release') {
      when {
        tag "v*"
      }
      
      steps {
        stepPublishArtifacts(folder: "src/bin/Release", bucket: "whatson-${env.TAG_NAME}", exclude: [], subfolders: false)
      }
    }
  }
}