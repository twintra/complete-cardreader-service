<template>
	<v-row justify="center" align="center">
		<v-col cols="12">
			<v-card>
				<v-card-text>
					<div>
						<v-row>
							<v-col>
								<div>Device: <strong class="primary--text">{{ device ?? '-' }}</strong></div>
								<div>Card status: <strong class="primary--text">{{ status ?? '-' }}</strong></div>
								<div>Message: <strong class="primary--text">{{ message ?? '-' }}</strong></div>
								<div>Error: <strong class="primary--text">{{ error ?? '-' }}</strong></div>
							</v-col>
						</v-row>
					</div>
					<v-divider class="py-3" />
					<div v-if="cardData != null">
						<h2>ข้อมูลอ่านบัตรประชาชน</h2>
						<div class="d-flex justify-content-center my-3">
							<div class="flex-grow-0 text-right px-3">
								<img v-if="cardData.image_url" :src="cardData.image_url" />
							</div>
							<div class="flex-grow-1">
								<div class="d-flex">
									<div>เลขที่บัตรประชาชน:</div>
									<div><strong class="primary--text">{{ cardData.cid || '' }}</strong></div>
								</div>
								<div class="d-flex">
									<div>ชื่อ:</div>
									<div>
										<strong class="primary--text">
											{{ cardData.th_prefix || '' }}
											{{ cardData.th_firstname || '' }}&nbsp;&nbsp;
											{{ cardData.th_lastname || '' }}
										</strong>
									</div>
								</div>
								<div class="d-flex">
									<div>วัน/เดือน/ปีเกิด:</div>
									<div><strong class="primary--text">{{ cardData.birthday || '' }}</strong></div>
								</div>
							</div>
						</div>
					</div>

				</v-card-text>
			</v-card>
		</v-col>
	</v-row>
</template>

<script>
import { w3cwebsocket } from "websocket";

export default {
	name: 'IndexPage',
	data() {
		return {
			offline: true,
			uri: 'ws://127.0.0.1:7023',
			socket: null,
			status: 'none',
			device: false,
			message: '',
			error: '',
			cardData: null,
		}
	},
	mounted() {
		this.init();
	},
	methods: {
		init() {
			console.log('init');
			this.socket = new w3cwebsocket(this.uri)
			this.socket.onopen = this.onopen;
			this.socket.onmessage = this.onmessage;
		},
		onopen() {
			console.log("socket start connect")
		},
		onmessage(message) {
			console.log("socket.message", message.data.length < 200 ? message.data : message.data.substring(0, 100));
			try {
				const res = JSON.parse(message.data);
				console.log("res", res);
				const { type, data } = res;
				if (type === 'device') {
					this.device = res.data;
				} else if (type === 'status') {
					this.status = res.data;
				} else if (type === 'message') {
					this.message = res.data;
				} else if (type === 'error') {
					this.error = res.data;
				} else if (type === 'data') {
					this.cardData = res;
				} else {
					console.log("else", res.data);
				}
			} catch (error) {
				console.log(message);
				this.error = error;
			}
		},

		onDestroyed() {
			// this.socket.send("disconnect");
			this.socket.close();
			this.socket = null;
		},

		refresh() {
			console.log("refreshing connection to socket")
			this.socket.send("disconnect");
			this.socket.close();
			this.socket = null;
			this.init();
		},

		reconnect() {
			console.log("reconnect")
			this.socket.close();
			this.socket = null;
			this.init();
		},

		getList() {
			console.log('getList');
			this.socket.send('showClientList')
		},
		getStatus() {
			console.log('getStatus', this.socket);
			this.socket.send('status')
		},
		getReader() {
			console.log('getReader');
			this.socket.send('reader')
		},
	}
}
</script>
