#!groovy
@Library('SoloPipeline@1.7')
import com.soloplan.*

def Bucket = "tools"
def outputDir = "src/bin/Release"
def msixDir = "src/bin/MSIX"
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
    WhatsONChannel = "${env.TAG_NAME ? 'stable' : (env.BRANCH_NAME == 'master' ? 'beta' : 'alpha')}"
    Version = "${env.TAG_NAME ?: "1.${versionMonth}.${env.BUILD_NUMBER}-${env.WhatsONChannel}"}"
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
        dir(msixDir) {
          deleteDir()
        }
        stepMSBuild(project: 'src/Soloplan.WhatsON.Package/WhatsON.wapproj',
                    platform: 'x64',
                    useCommonOutput: false,
                    additionalParameters: "/p:RuntimeIdentifier=win-x64 /p:GenerateAppxPackageOnBuild=true /p:AppxPackageDir=\"${env.WORKSPACE}/${msixDir}/\"")
        script {
          if (env.BRANCH_NAME in ['develop', 'master'] || env.TAG_NAME) {
            stepUseCustomTool(tool: 'AzureSignTool', version: '6.0.1')
            withCredentials([
              string(credentialsId: 'SOLOPLAN_EV_CODE_SIGNING_CERTIFICATE_NAME', variable: 'SOLOPLAN_EV_CODE_SIGNING_CERTIFICATE_NAME'),
              string(credentialsId: 'AZURE_SERVICE_PRINCIPAL_SECRET', variable: 'AZURE_SERVICE_PRINCIPAL_SECRET')
            ]) {
              def packages = findFiles(glob: "${msixDir}/**/*.msix").collect { it.path }
              if (!packages) {
                error('No MSIX package was generated.')
              }
              stepAzureSign(files: packages)
            }
          }
        }
        archiveArtifacts artifacts: "${msixDir}/**/*.msix,${msixDir}/whatsON.appinstaller", fingerprint: true
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
        stepPublishArtifacts(bucket: Bucket, targetFolder: "whatson/${env.WhatsONChannel}", folder: outputDir, exclude: ['*.pdb'], excludeSubfolders: false)
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
