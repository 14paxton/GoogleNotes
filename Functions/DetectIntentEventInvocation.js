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
    const cxClient = new SessionsClient();
    const projectId = "dialogflow-378918";
    const location = "global";
    const agentId = "069075ae-67c3-4223-88cc-137335336001";
    const languageCode = "en";
    const event = "initiate-bot-event";
    const sessionId = "this_is_a_session_id_123456";
    const sessionPath = cxClient.projectLocationAgentSessionPath(
      projectId,
      location,
      agentId,
      sessionId
    );

    // Construct detect intent request
    const request = {
      session: sessionPath,
      queryInput: {
        event: {
          event: event,
        },
        languageCode,
      },
    };

    // Send request and receive response
    const [response] = await cxClient.detectIntent(request);
    console.log(`Event Name: ${event}`);

    // Response message from the triggered event
    console.log("Agent Response: \n");
    console.log(response.queryResult.responseMessages[0].text.text[0]);

    return JSON.stringify(response.queryResult);
  }

  detectIntentText().then((resp) => {
    console.debug(resp);
    res.status(200).send(resp);
  });
};
