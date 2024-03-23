pipeline {
    agent any
	options {
        ansiColor('xterm')
    }

    stages {
        stage('Checkout') {
            steps {
                git branch: env.BRANCH_NAME, credentialsId: 'GitHub PAT', url: 'https://github.com/nullinside-development-group/nullinside-api.git'
            }
        }
        
        stage('Build & Deploy') {
            steps {
				withCredentials([
					usernamePassword(credentialsId: 'Docker', passwordVariable: 'DOCKER_PASSWORD', usernameVariable: 'DOCKER_USERNAME'),
					usernamePassword(credentialsId: 'Docker2', passwordVariable: 'DOCKER_PASSWORD2', usernameVariable: 'DOCKER_USERNAME2'),
					string(credentialsId: 'DockerServer', variable: 'DOCKER_SERVER'),
					usernamePassword(credentialsId: 'MySql', passwordVariable: 'MYSQL_PASSWORD', usernameVariable: 'MYSQL_USERNAME'),
					string(credentialsId: 'MySqlServer', variable: 'MYSQL_SERVER'),
					string(credentialsId: 'TwitchBotClientId', variable: 'TWITCH_BOT_CLIENT_ID'),
					string(credentialsId: 'TwitchBotClientSecret', variable: 'TWITCH_BOT_CLIENT_SECRET'),
					string(credentialsId: 'TwitchBotClientRedirect', variable: 'TWITCH_BOT_CLIENT_REDIRECT')
				]) {
					sh """
						bash go.sh 
					"""
				}
            }
        }
    }
	
	post {
		always {
			cleanWs cleanWhenFailure: false, notFailBuild: true
		}
	}
}
