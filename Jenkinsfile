pipeline {
  agent any

  parameters {
    string(name: 'PROMPT', description: 'Prompt for Codex (민감 내용이면 최소화)')
    string(name: 'GIT_REPO_URL', description: 'SSH URL 권장: git@github.com:owner/repo.git')
    string(name: 'GIT_BRANCH', defaultValue: 'master', description: 'Git branch to checkout')
    string(name: 'GIT_USER_NAME', defaultValue: 'Jenkins CI', description: 'Git user name for commits')
    string(name: 'GIT_USER_EMAIL', defaultValue: 'jenkins@example.com', description: 'Git user email for commits')
    string(name: 'MODEL', defaultValue: 'openai/gpt-4.1', description: 'Codex model (로컬 설정에 맞게)')
    string(name: 'PROVIDER', defaultValue: 'openai', description: 'Codex provider (로컬 설정에 맞게)')
    booleanParam(name: 'ENABLE_GIT_PUSH', defaultValue: false, description: '변경사항을 codex-build-<BUILD_NUMBER> 브랜치로 push')
  }

  stages {

    stage('Validate Parameters') {
      steps {
        script {
          if (!params.PROMPT?.trim())       error "PROMPT parameter is required."
          if (!params.GIT_REPO_URL?.trim()) error "GIT_REPO_URL parameter is required."
          if (!params.GIT_BRANCH?.trim())   error "GIT_BRANCH parameter is required."
        }
      }
    }

    stage('Initialize Workspace') {
      steps {
        script {
          echo "Init workspace for ${params.GIT_REPO_URL} @ ${params.GIT_BRANCH}"

          // safe.directory 설정 (루트/권한 이슈 방지)
          sh '''
            git config --global --add safe.directory "$(pwd)" || true
          '''

          // SSH 키가 이미 세팅돼 있으므로 바로 git 사용
          sh """
            rm -rf .git || true
            git init
            git remote add origin "${params.GIT_REPO_URL}"
            git fetch --depth=0 origin ${params.GIT_BRANCH}
            git checkout -B ${params.GIT_BRANCH} origin/${params.GIT_BRANCH}
            git reset --hard origin/${params.GIT_BRANCH}
            git clean -fdx
            git status
          """
        }
      }
    }

    stage('Invoke Codex') {
      steps {
        script {
          // 프롬프트 길이 제한 출력
          def preview = params.PROMPT.size() > 160 ? params.PROMPT.take(160) + ' ...[truncated]' : params.PROMPT
          echo "Invoking Codex (model=${params.MODEL}, provider=${params.PROVIDER})"
          echo "Prompt preview: ${preview}"

          // Codex 실행 (Plus 계정 로컬 인증 기반)
          sh """
            codex "${params.PROMPT}" --model "${params.MODEL}" --provider "${params.PROVIDER}" -a auto-edit --quiet
          """
        }
      }
    }

    stage('Check for Git Changes') {
      steps {
        script {
          echo "Checking for Codex changes..."
          def changes = sh(script: 'git status --porcelain', returnStdout: true).trim()
          env.CHANGES_DETECTED = changes ? "true" : "false"
          if (changes) {
            sh 'git status --short'
          } else {
            echo "No changes."
          }
        }
      }
    }

    stage('Commit and (Optional) Push') {
      when { expression { env.CHANGES_DETECTED == "true" } }
      steps {
        script {
          def branchName = "codex-build-${env.BUILD_NUMBER}"
          echo "Commit & push to ${branchName} (ENABLE_GIT_PUSH=${params.ENABLE_GIT_PUSH})"

          sh """
            git config user.name  '${params.GIT_USER_NAME}'  || true
            git config user.email '${params.GIT_USER_EMAIL}' || true
            git checkout -b ${branchName}
            git add -A
            git commit -m "Codex changes (Build ${BUILD_NUMBER})"
          """

          if (params.ENABLE_GIT_PUSH) {
            // 이미 서버에 SSH 키 세팅되어 있으므로 바로 push
            sh "git push -u origin ${branchName}"
            echo "Pushed ${branchName}."
          } else {
            echo "Skip push (ENABLE_GIT_PUSH=false)."
          }
        }
      }
    }
  }

  post {
    always  { echo 'Pipeline finished.' }
    success { echo 'Pipeline completed successfully.' }
    failure { echo 'Pipeline failed.' }
  }
}
