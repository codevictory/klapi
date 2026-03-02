import OAuth from "start-oauth";
import { createUser, findUser } from "~/auth/db";
import { createSession, getSessionSecret } from "~/auth/server";

export const GET = OAuth({
  password: getSessionSecret(),
  async handler({ email }, redirectTo) {
    let user = await findUser({ email });
    if (!user) user = await createUser({ email });
    return createSession(user, redirectTo);
  },
});
