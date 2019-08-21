#!groovy​
@Library('SoloPipeline@1.0-rc7')
import com.soloplan.*

def Bucket = "whatson"
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
        stepPublishArtifacts(bucket: Bucket, targetFolder: "master", folder: "src/bin/Release",  exclude: ['*.deps.json'], excludeSubfolders: false)
      }
    }

    stage('Publish Release') {
      when {
        tag "v*"
      }
      
      steps {
        stepPublishArtifacts(bucket: Bucket, targetFolder: env.TAG_NAME, folder: "src/bin/Release", exclude: ['*.deps.json', '*.pdb'], excludeSubfolders: false)
      }
    }
  }
}