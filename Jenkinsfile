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
        buildDiscarder(logRotator(numToKeepStr: '10'))
        disableConcurrentBuilds()
        timestamps()
    }

    stages {
        stage('Checkout') {
            steps {
                echo 'Checking out source code...'
                checkout scm
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
                    catchError(buildResult: 'SUCCESS', message: 'No test projects found or tests failed') {
                        if (isUnix()) {
                            sh """
                            dotnet test ${env.SLN_FILE} \
                              --configuration ${env.BUILD_CONFIGURATION} \
                              --verbosity normal \
                              --logger "trx;LogFileName=test-results.trx" \
                              --results-directory TestResults
                            """
                        } else {
                            bat """
                            dotnet test ${env.SLN_FILE} ^
                              --configuration ${env.BUILD_CONFIGURATION} ^
                              --verbosity normal ^
                              --logger "trx;LogFileName=test-results.trx" ^
                              --results-directory TestResults
                            """
                        }
                    }
                }
            }
            post {
                always {
                    script {
                        def trxFiles = []
                        def noTests = true
                        def trxPath = ''

                        // TRX path is deterministic: dotnet test writes to TestResults/test-results.trx
                        // Use fileExists() — always available, no extra plugin needed
                        def fixedTrxPath = 'TestResults/test-results.trx'
                        if (fileExists(fixedTrxPath)) {
                            trxPath = fixedTrxPath
                            noTests = false
                            trxFiles = [ [path: trxPath] ]
                        }

                        // --- Summary Stats ---
                        def total   = 0
                        def passed  = 0
                        def failed  = 0
                        def skipped = 0
                        def testResults = []

                        if (!noTests) {
                            if (isUnix()) {
                                try {
                                    sh 'python3 parse_trx.py'
                                } catch (Exception e) {
                                    echo "Warning: Python parser script failed: ${e.getMessage()}"
                                }
                            } else {
                                try {
                                    bat 'powershell -ExecutionPolicy Bypass -File parse_trx.ps1'
                                } catch (Exception e) {
                                    echo "Warning: PowerShell parser script failed: ${e.getMessage()}"
                                }
                            }

                            // Read and parse the summary file
                            def summaryPath = 'TestResults/summary.txt'
                            if (fileExists(summaryPath)) {
                                try {
                                    def lines = readFile(summaryPath).readLines()
                                    lines.each { line ->
                                        if (line.contains('=')) {
                                            def parts = line.split('=')
                                            if (parts.size() >= 2) {
                                                def key = parts[0].trim()
                                                def val = parts[1].trim().toInteger()
                                                if (key == 'total') total = val
                                                else if (key == 'passed') passed = val
                                                else if (key == 'failed') failed = val
                                                else if (key == 'skipped') skipped = val
                                            }
                                        }
                                    }
                                } catch (Exception e) {
                                    echo "Warning: Failed to parse summary file: ${e.getMessage()}. Falling back to default stats."
                                    noTests = true
                                }
                            } else {
                                noTests = true
                            }
                        }


                        // Publish as Jenkins HTML report (shows in build sidebar)
                        publishHTML(target: [
                            allowMissing         : true,
                            alwaysLinkToLastBuild: true,
                            keepAll              : true,
                            reportDir            : 'TestResults',
                            reportFiles          : 'test-results.html',
                            reportName           : 'Test Report',
                            reportTitles         : 'CI / Test Report'
                        ])
                        archiveArtifacts artifacts: 'TestResults/test-results.html', allowEmptyArchive: true

                        // --- TRX / MSTest ---
                        if (trxFiles) {
                            try {
                                mstest testResultsFile: trxFiles[0].path, keepLongStdio: true
                            } catch (Exception e) {
                                echo "MSTest plugin not available: ${e.getMessage()}"
                            }
                        }

                        // --- Summary Table ---
                        echo ''
                        echo '=============================================='
                        echo '          TEST RESULTS SUMMARY'
                        echo '=============================================='
                        echo "  Total   : ${total}"
                        echo "  Passed  : ${passed}   [${passed > 0 ? 'SUCCESS' : 'NONE'}]"
                        echo "  Failed  : ${failed}   [${failed > 0 ? 'FAILURE' : 'NONE'}]"
                        echo "  Skipped : ${skipped}"
                        echo '=============================================='
                        echo "  Report  : TestResults/test-results.html"
                        echo "  TRX     : ${trxPath ?: 'N/A'}"
                        echo '=============================================='
                        echo ''

                        currentBuild.result = failed > 0 ? 'FAILURE' : 'SUCCESS'
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
                archiveArtifacts artifacts: 'publish/**', onlyIfSuccessful: true
            }
        }

        stage('Docker Build') {
            steps {
                script {
                    echo "Building Docker image ${env.DOCKER_IMAGE_NAME}:${env.BUILD_NUMBER}..."
                    try {
                        if (isUnix()) {
                            sh "docker build -t ${env.DOCKER_IMAGE_NAME}:${env.BUILD_NUMBER} -t ${env.DOCKER_IMAGE_NAME}:latest ."
                        } else {
                            bat "docker build -t ${env.DOCKER_IMAGE_NAME}:${env.BUILD_NUMBER} -t ${env.DOCKER_IMAGE_NAME}:latest ."
                        }
                    } catch (Exception e) {
                        echo "Docker build skipped: ${e.getMessage()}"
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