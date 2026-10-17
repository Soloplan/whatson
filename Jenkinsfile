#!groovy​
@Library('SoloPipeline@1.0-rc11')
import com.soloplan.*

def Bucket = "tools"
def outputDir = "src/bin/Release"

pipeline {
  agent {
    // This Windows agent must have the .NET 10 SDK installed.
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
        bat 'dotnet build src/Soloplan.WhatsON.slnx --configuration Release'
      }
    }

    stage('Test') {
      steps {
        dir('src/bin/TestResults') {
          deleteDir()
        }
        bat 'dotnet test src/Soloplan.WhatsON.slnx --configuration Release --no-build --logger trx --results-directory src/bin/TestResults'
      }
      post {
        always {
          archiveArtifacts artifacts: 'src/bin/TestResults/*.trx', allowEmptyArchive: true
        }
      }
    }

    stage('Package') {
      steps {
        dir(outputDir) {
          deleteDir()
        }
        bat "dotnet publish src/Soloplan.WhatsON.GUI/Soloplan.WhatsON.GUI.csproj --configuration Release --no-build --output ${outputDir}"
      }
    }

    stage('Publish Snapshot') {
      when {
        branch 'master'
      }
      
      steps {
        stepPublishArtifacts(bucket: Bucket, targetFolder: "whatson/master", folder: outputDir, exclude: ['*.pdb'], excludeSubfolders: false)
      }
    }

    stage('Publish Release') {
      when {
        tag "v*"
      }
      
      steps {
        stepPublishArtifacts(bucket: Bucket, targetFolder: "whatson/${env.TAG_NAME}", folder: outputDir, exclude: ['*.pdb'], excludeSubfolders: false)
      }
    }
  }
}