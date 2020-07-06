#!groovy​
@Library('SoloPipeline@1.0-rc11')
import com.soloplan.*

def Bucket = "tools"
def outputDir = "src/bin/Release"

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
        stepMSBuild(project: 'src/Soloplan.WhatsON.sln', outputDir: outputDir)
      }
    }

    stage('Test') {
      steps {
        stepNunit(folder: outputDir)
      }
    }

    stage('Publish Snapshot') {
      when {
        branch 'master'
      }
      
      steps {
        stepPublishArtifacts(bucket: Bucket, targetFolder: "whatson/master", folder: outputDir,  exclude: ['*.deps.json', '*.Tests.dll', '.nupkg'], excludeSubfolders: false)
      }
    }

    stage('Publish Release') {
      when {
        tag "v*"
      }
      
      steps {
        stepPublishArtifacts(bucket: Bucket, targetFolder: "whatson/${env.TAG_NAME}", folder: outputDir, exclude: ['*.deps.json', '*.pdb', '*.Tests.dll', '.nupkg'], excludeSubfolders: false)
      }
    }
  }
}