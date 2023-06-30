<template>
  <v-row justify="center" align="center">
    <v-col cols="12">
      <v-card>
        <v-card-text>
          <div>
            <v-row>
              <v-col>
                <div>Device: <strong class="primary--text">{{ device ?? '-' }}</strong></div>
                <!-- <div>Status: <strong class="primary--text">{{ status ?? '-' }}</strong></div> -->
                <div>Message: <strong class="primary--text">{{ message ?? '-' }}</strong></div>
                <div>Error: <strong class="danger--text">{{ error ?? '-' }}</strong></div>
              </v-col>
            </v-row>
          </div>
          <v-divider class="py-3" />
          <div v-if="cardData != null">
            <h2>ข้อมูลอ่านบัตรประชาชน</h2>
            <v-row>
              <v-col>
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
              </v-col>
              <v-col>
                <v-card style="max-width: 30vw;" class="pa-2 grey lighten-4">
                  <pre>{{ JSON.stringify(cardData, 2, 2) }}</pre>
                </v-card>
              </v-col>
            </v-row>
          </div>

        </v-card-text>
      </v-card>
    </v-col>
  </v-row>
</template>

<script>
export default {
  name: 'IndexPage',
  data() {
    return {
      offline: true,
      uri: 'http://localhost:8996/reader/get',
      service: null,
      status: '',
      device: '',
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
      this.getReader();
      this.service = setInterval(this.getReader, 2 * 1000)
    },
    async getReader() {

      await fetch(this.uri).then((res) => res.json()).then((res) => {

        console.log(res);

        const { status, data, message, device } = res;
        this.status = status;
        this.device = device;

        if (status == "ok") {
          this.error = '';
          this.message = message;
          if (message == "inserted") {
            this.cardData = data;
            if (data.photo) this.cardData.image_url = "data:image/png;base64," + data.photo;
          } else {
            this.cardData = null;
          }
        } else {
          this.message = '';
          this.error = message;
          this.cardData = null;
        }

      })

    },
  },
  beforeDestroy() {
    clearInterval(this.service);
    this.service = null;
  }
}
</script>
