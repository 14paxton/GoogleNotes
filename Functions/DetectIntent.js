/**
 * Responds to any HTTP request.
 *
 * @param {!express:Request} req HTTP request context.
 * @param {!express:Response} res HTTP response context.
 */
const { SessionsClient } = require("@google-cloud/dialogflow-cx");
exports.detectintent = (req, res) => {
  console.log("res", res);
  console.log("req", req);

  async function detectIntentText() {
    const client = new SessionsClient();
    let message = "Hello World!";
    const projectId = "dialogflow-378918";
    const location = "global";
    const agentId = "069075ae-67c3-4223-88cc-137335336001";
    const languageCode = "en";
    const event = "initiate-bot-event";
    const sessionId = "this_is_a_session_id_123456";
    const sessionPath = client.projectLocationAgentSessionPath(
      projectId,
      location,
      agentId,
      sessionId
    );
    const request = {
      session: sessionPath,
      queryInput: {
        text: {
          text: message,
        },
        languageCode,
      },
    };

    const [response] = await client.detectIntent(request);

    for (const message of response.queryResult.responseMessages) {
      if (message.text) {
        console.log(`Agent Response: ${message.text.text}`);
      }
    }

    if (response.queryResult.match.intent) {
      console.log(
        `Matched Intent: ${response.queryResult.match.intent.displayName}`
      );
    }

    console.log(
      `Current Page: ${response.queryResult.currentPage.displayName}`
    );

    res.status(200).send(response.queryResult);
  }

  detectIntentText().then((resp) => {
    console.debug(resp);
  });
};
