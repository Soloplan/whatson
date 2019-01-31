#!groovy​
@Library('SoloPipeline')
import com.soloplan.*

pipeline {
  agent {
    label 'net-framework-sdk'
  } 

  stages {
    stage('Agent info') {
      steps {
        stepAgentInfo()
      }
    }

    stage('Build') {
      steps {
        stepMSBuild(solution: 'src/Soloplan.WhatsON.sln')
      }
    }
  }

  post {
    success {
      stepArchiveArtifacts()
      stepPublishArtifacts(bucket: "whatson-${env.JOB_BASE_NAME}")
    }
  }
}