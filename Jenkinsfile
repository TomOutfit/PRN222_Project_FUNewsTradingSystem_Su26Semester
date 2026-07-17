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
                    catchError(buildResult: 'SUCCESS', message: 'No test projects found or tests failed') {
                        if (isUnix()) {
                            sh """
                            dotnet test ${env.SLN_FILE} \
                              --configuration ${env.BUILD_CONFIGURATION} \
                              --verbosity normal \
                              --logger "trx;LogFileName=test-results.trx" \
                              --results-directory TestResults \
                              --logger "html;LogFileName=test-results.html"
                            """
                        } else {
                            bat """
                            dotnet test ${env.SLN_FILE} ^
                              --configuration ${env.BUILD_CONFIGURATION} ^
                              --verbosity normal ^
                              --logger "trx;LogFileName=test-results.trx" ^
                              --results-directory TestResults ^
                              --logger "html;LogFileName=test-results.html"
                            """
                        }
                    }
                }
            }
            post {
                always {
                    script {
                        def trxFiles  = findFiles(glob: '**/TestResults/*.trx')
                        def htmlFiles = findFiles(glob: '**/TestResults/*.html')

                        // --- HTML Report ---
                        if (htmlFiles) {
                            publishHTML([
                                allowMissing: false,
                                alwaysLinkToLastBuild: true,
                                keepAll: true,
                                reportDir: 'TestResults',
                                reportFiles: htmlFiles[0].name,
                                reportName: 'Test Results — HTML Report'
                            ])
                        } else {
                            echo '[Test] No HTML test report found (no test project or tests not run).'
                        }

                        // --- TRX / MSTest ---
                        if (trxFiles) {
                            try {
                                mstest testResultsFile: trxFiles[0].path, keepLongStdio: true
                            } catch (Exception e) {
                                echo "MSTest plugin not available: ${e.getMessage()}"
                            }
                        }

                        // --- Summary Table ---
                        def total  = 0
                        def passed = 0
                        def failed = 0
                        def skipped = 0

                        if (trxFiles) {
                            def xml = new XmlSlurper().parse(new FileReader(trxFiles[0].path))
                            total   = xml.TestRun.TestDefinitions.TestMethod.size()
                            def counters = xml.TestRun.Results.Counters.'#text'.toString().trim().split('\\s+')
                            passed  = counters.size() > 0 ? counters[0].toInteger() : 0
                            failed  = counters.size() > 1 ? counters[1].toInteger() : 0
                            skipped = counters.size() > 2 ? counters[2].toInteger() : 0
                        }

                        echo ''
                        echo '=============================================='
                        echo '          TEST RESULTS SUMMARY'
                        echo '=============================================='
                        echo "  Total   : ${total}"
                        echo "  Passed  : ${passed}   [${passed > 0 ? 'SUCCESS' : 'NONE'}]"
                        echo "  Failed  : ${failed}   [${failed > 0 ? 'FAILURE' : 'NONE'}]"
                        echo "  Skipped : ${skipped}"
                        echo '=============================================='
                        echo "  Report  : ${htmlFiles ? htmlFiles[0].path : 'N/A'}"
                        echo "  TRX     : ${trxFiles  ? trxFiles[0].path  : 'N/A'}"
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
