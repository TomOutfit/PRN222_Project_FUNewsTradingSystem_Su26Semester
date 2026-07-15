pipeline {
    agent any

    environment {
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        BUILD_CONFIGURATION         = 'Release'
        SLN_FILE                    = 'FUNewsTradingSystem.sln'
        MVC_PROJECT                 = 'FUNewsTradingSystem/MVC/MVC.csproj'
        DOCKER_IMAGE_NAME           = 'funewstradingsystem'
    }

    options {
        timeout(time: 30, unit: 'MINUTES')
        keepMaxBuilds(10)
        disableConcurrentBuilds()
        ansiColor('xterm')
    }

    stages {
        stage('Checkout') {
            steps {
                echo 'Checking out source code...'
            }
        }

        stage('Environment Info') {
            steps {
                script {
                    echo "Running on OS: ${isUnix() ? 'Unix-like' : 'Windows'}"
                    try {
                        if (isUnix()) {
                            sh 'dotnet --version'
                            sh 'docker --version'
                        } else {
                            bat 'dotnet --version'
                            bat 'docker --version'
                        }
                    } catch (Exception e) {
                        echo "Warning: dotnet or docker tools might not be in the PATH. Details: ${e.getMessage()}"
                    }
                }
            }
        }

        stage('Restore Dependencies') {
            steps {
                script {
                    if (isUnix()) {
                        sh "dotnet restore ${env.SLN_FILE}"
                    } else {
                        bat "dotnet restore ${env.SLN_FILE}"
                    }
                }
            }
        }

        stage('Build') {
            steps {
                script {
                    if (isUnix()) {
                        sh "dotnet build ${env.SLN_FILE} --configuration ${env.BUILD_CONFIGURATION} --no-restore"
                    } else {
                        bat "dotnet build ${env.SLN_FILE} --configuration ${env.BUILD_CONFIGURATION} --no-restore"
                    }
                }
            }
        }

        stage('Test') {
            steps {
                script {
                    echo 'Scanning and running test projects...'
                    if (isUnix()) {
                        sh "dotnet test ${env.SLN_FILE} --configuration ${env.BUILD_CONFIGURATION} --no-build --verbosity normal --logger \"trx;LogFileName=test-results.trx\" --results-directory TestResults"
                    } else {
                        bat "dotnet test ${env.SLN_FILE} --configuration ${env.BUILD_CONFIGURATION} --no-build --verbosity normal --logger \"trx;LogFileName=test-results.trx\" --results-directory TestResults"
                    }
                }
            }
            post {
                always {
                    script {
                        try {
                            mstest testResultsFile: '**/TestResults/*.trx', keepLongStdio: true
                        } catch (Exception e) {
                            echo "No test results (.trx) found or MSTest plugin is not installed: ${e.getMessage()}"
                        }
                    }
                }
            }
        }

        stage('Publish Artifacts') {
            steps {
                script {
                    if (isUnix()) {
                        sh "dotnet publish ${env.MVC_PROJECT} -c ${env.BUILD_CONFIGURATION} -o ./publish"
                    } else {
                        bat "dotnet publish ${env.MVC_PROJECT} -c ${env.BUILD_CONFIGURATION} -o ./publish"
                    }
                }
                // Archive the published outputs in Jenkins
                archiveArtifacts artifacts: 'publish/**', onlyIfSuccessful: true
            }
        }

        stage('Docker Build') {
            steps {
                script {
                    echo "Building Docker image ${env.DOCKER_IMAGE_NAME}:${env.BUILD_NUMBER}..."
                    if (isUnix()) {
                        sh "docker build -t ${env.DOCKER_IMAGE_NAME}:${env.BUILD_NUMBER} -t ${env.DOCKER_IMAGE_NAME}:latest ."
                    } else {
                        bat "docker build -t ${env.DOCKER_IMAGE_NAME}:${env.BUILD_NUMBER} -t ${env.DOCKER_IMAGE_NAME}:latest ."
                    }
                }
            }
        }
    }

    post {
        always {
            echo 'Pipeline execution completed.'
        }
        success {
            echo 'Build and deployment preparation succeeded!'
        }
        failure {
            echo 'Build failed. Please check the logs above.'
        }
        unstable {
            echo 'Pipeline completed with test failures (unstable).'
        }
        cleanup {
            cleanWs()
        }
    }
}
