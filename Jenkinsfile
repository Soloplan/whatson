#!groovy
@Library('SoloPipeline@1.7')
import com.soloplan.*

def Bucket = "tools"
def outputDir = "src/bin/Release"
def solution = "src/Soloplan.WhatsON.slnx"
def versionMonth = new Date(currentBuild.startTimeInMillis).format('yyMM', TimeZone.getTimeZone('UTC'))

pipeline {
  agent {
    // This Windows agent must have the .NET 10 SDK installed.
    label 'dotnet-framework'
  }

  options {
    buildDiscarder(logRotator(numToKeepStr: '100'))
    disableConcurrentBuilds()
    disableResume()
    skipStagesAfterUnstable()
    timestamps()
  }

  environment {
    Version = "${env.TAG_NAME ?: "1.${versionMonth}.${env.BUILD_NUMBER}-${env.BRANCH_NAME.replaceAll('[^0-9A-Za-z-]', '-')}"}"
  }

  stages {
    stage('Agent info') {
      steps {
        stepAgentInfo()
      }
    }

    stage('Build') {
      steps {
        stepMSBuild(project: solution, useCommonOutput: false)
      }
    }

    stage('Test') {
      steps {
        dir('TestResults') {
          deleteDir()
        }
        stepDotNetTest(tests: [solution],
                       exclude: ['Integration', 'UI'],
                       resultFilePrefix: 'NUnit',
                       formats: ['nunit'])
      }
      post {
        always {
          archiveArtifacts artifacts: 'TestResults/NUnit/*.xml', allowEmptyArchive: true
        }
      }
    }

    stage('Package') {
      steps {
        dir(outputDir) {
          deleteDir()
        }
        stepMSBuild(project: 'src/Soloplan.WhatsON.GUI/Soloplan.WhatsON.GUI.csproj',
                    buildTargets: 'Publish',
                    useCommonOutput: false,
                    publishDir: outputDir)
      }
    }

    stage('Publish Snapshot') {
      when {
        anyOf {
          branch 'master'
          branch 'develop'
        }
      }
      
      steps {
        stepPublishArtifacts(bucket: Bucket, targetFolder: "whatson/${env.BRANCH_NAME}", folder: outputDir, exclude: ['*.pdb'], excludeSubfolders: false)
      }
    }

    stage('Publish Release') {
      when {
        buildingTag()
      }
      
      steps {
        stepPublishArtifacts(bucket: Bucket, targetFolder: "whatson/${env.TAG_NAME}", folder: outputDir, exclude: ['*.pdb'], excludeSubfolders: false)
      }
    }
  }
}
