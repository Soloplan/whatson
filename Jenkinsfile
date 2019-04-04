#!groovy​
@Library('SoloPipeline')
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

    stage('Publish') {
      when {
        branch 'master'
      }
      
      steps {
        stepPublishArtifacts(folder: "src/bin/Release", bucket: "whatson", exclude: [])
      }
    }
  }
}